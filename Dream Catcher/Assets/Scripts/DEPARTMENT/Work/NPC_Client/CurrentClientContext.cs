public static class CurrentClientContext
{
    public static VisitorCaseData CurrentClient
    {
        get;
        private set;
    }

    public static VisitorCaseData.VisitorCaseVariant
        CurrentVariant
    {
        get;
        private set;
    }

    public static bool HasCurrentCase =>
        CurrentClient != null &&
        CurrentVariant != null;

    public static void SetCurrentCase(
        VisitorCaseData client,
        VisitorCaseData.VisitorCaseVariant variant)
    {
        CurrentClient = client;
        CurrentVariant = variant;
    }

    public static void Clear()
    {
        CurrentClient = null;
        CurrentVariant = null;
    }
}