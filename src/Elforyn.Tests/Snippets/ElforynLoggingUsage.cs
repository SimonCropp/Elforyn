class ElforynLoggingUsage
{
    ElforynLoggingUsage()
    {
        #region ElforynLoggingUsage

        ElforynLogging.EnableVerbose();

        #endregion

        #region ElforynLoggingUsageSqlLogging

        ElforynLogging.EnableVerbose(sqlLogging: true);

        #endregion
    }
}
