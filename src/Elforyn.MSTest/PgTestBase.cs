namespace ElforynMSTest;

[TestClass]
[UsesVerify]
public abstract partial class PgTestBase<T> :
    ILocalDbTestBase
    where T : DbContext
{
    Phase phase = Phase.Arrange;
    static PgInstance<T> pgInstance = null!;
    T actData = null!;
    T arrangeData = null!;

    bool isSharedDb;
    bool isSharedDbWithTransaction;

    public static void Initialize(
        string connectionString,
        ConstructInstance<T>? constructInstance = null,
        TemplateFromContext<T>? buildTemplate = null,
        Callback<T>? callback = null,
        DateTime? timestamp = null)
    {
        var callingAssembly = Assembly.GetCallingAssembly();
        ThrowIfInitialized();
        pgInstance = new(
            connectionString: connectionString,
            buildTemplate: buildTemplate,
            constructInstance: builder =>
            {
                builder.EnableRecording();
                return constructInstance == null ? BuildDbContext(builder) : constructInstance(builder);
            },
            storage: GetStorage(callingAssembly),
            callback: callback,
            timestamp: timestamp);
    }

    [TestInitialize]
    public virtual Task SetUp()
    {
        if (pgInstance == null)
        {
            throw new("Call PgTestBase<T>.Initialize in a [ModuleInitializer] or in a static constructor.");
        }

        var methodInfo = GetType()
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .First(_ => _.Name == TestContext.TestName && !_.IsGenericMethod);
        isSharedDbWithTransaction = methodInfo.GetCustomAttribute<SharedDbWithTransactionAttribute>() != null;
        var hasSharedDbAttribute = methodInfo.GetCustomAttribute<SharedDbAttribute>() != null;

        if (isSharedDbWithTransaction && hasSharedDbAttribute)
        {
            throw new("[SharedDb] and [SharedDbWithTransaction] are mutually exclusive. Use only one on a test method.");
        }

        isSharedDb = isSharedDbWithTransaction || hasSharedDbAttribute;

        QueryFilter.Enable();
        return Reset();
    }

    public async Task Reset()
    {
        phase = Phase.Arrange;
        var type = GetType().FullName!;
        var member = TestContext.TestName;
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if(Database != null)
        {
            if (isSharedDb)
            {
                await Database.DisposeAsync();
            }
            else
            {
                await Database.Delete();
                await Database.DisposeAsync();
            }
        }

        Database = isSharedDb
            ? await pgInstance.BuildShared(useTransaction: isSharedDbWithTransaction)
            : await pgInstance.Build(type, null, member);

        Database.NoTrackingContext.DisableRecording();
        arrangeData = Database.Context;
        arrangeData.DisableRecording();
        actData = Database.NewDbContext();

        if (isSharedDbWithTransaction)
        {
            await actData.Database.UseTransactionAsync(Database.Transaction);
        }
    }

    public static async Task Shutdown()
    {
        // ReSharper disable once ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
        if (pgInstance != null)
        {
            await pgInstance.Cleanup();
        }

        pgInstance = null!;
    }

    static void ThrowIfInitialized()
    {
        if (pgInstance != null)
        {
            throw new("Already initialized.");
        }
    }

    static T BuildDbContext(DbContextOptionsBuilder<T> builder)
    {
        var type = typeof(T);
        try
        {
            return (T)Activator.CreateInstance(type, builder.Options)!;
        }
        catch (Exception exception)
        {
            throw new($"Could not construct instance of T ({type.Name}). Either provide a constructInstance delegate or ensure T has a constructor that accepts DbContextOptions.", exception);
        }
    }

    static Storage GetStorage(Assembly callingAssembly) =>
        Storage.FromSuffix<T>($"{AttributeReader.GetSolutionName(callingAssembly)}_{AttributeReader.GetProjectName(callingAssembly)}");

    public PgDatabase<T> Database { get; private set; } = null!;

    public virtual T ArrangeData
    {
        get
        {
            if (phase == Phase.Act)
            {
                throw new("Phase has already moved to Act. Check for a ActData usage in the preceding code.");
            }

            if (phase == Phase.Assert)
            {
                throw new("Phase has already moved to Assert. Check for a AssertData usage in the preceding code.");
            }

            return arrangeData;
        }
    }

    public virtual T ActData
    {
        get
        {
            if (phase == Phase.Act)
            {
                return actData;
            }

            if (phase == Phase.Assert)
            {
                throw new("Phase has already moved to Assert. Check for a AssertData usage in the preceding code.");
            }

            Recording.Resume();
            phase = Phase.Act;
            arrangeData.Dispose();
            return actData;
        }
    }

    public virtual T AssertData
    {
        get
        {
            if (phase == Phase.Assert)
            {
                return Database.NoTrackingContext;
            }

            phase = Phase.Assert;
            arrangeData.Dispose();
            actData.Dispose();

            QueryFilter.Disable();
            if (Recording.IsRecording())
            {
                Recording.Pause();
            }

            return Database.NoTrackingContext;
        }
    }

    protected PgTestBase()
    {
        CombinationCallback.SetInstance(this);
        // Enable and Recording needs to be at the top of the AsyncLocal stack
        QueryFilter.Enable();
        Recording.Start();
        Recording.Pause();
        instance.Value = this;
    }

    static AsyncLocal<PgTestBase<T>?> instance = new();

    public static PgTestBase<T> Instance
    {
        get
        {
            var value = instance.Value;
            if (value == null)
            {
                throw new("No current value");
            }

            return value;
        }
    }

    [TestCleanup]
    public virtual async Task TearDown()
    {
        // ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract

        if (actData != null)
        {
            await actData.DisposeAsync();
        }

        if (Database != null)
        {
            if (!isSharedDb && BuildServerDetector.Detected)
            {
                ElforynLogging.LogIfVerbose($"Purging {Database.Name}");
                await Database.Delete();
            }
            else
            {
                await Database.DisposeAsync();
            }
        }

        // ReSharper restore ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        instance.Value = null;
    }
}
