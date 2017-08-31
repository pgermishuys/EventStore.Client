namespace EventStore.Client.SystemData
{
    internal enum InspectionDecision
    {
        DoNothing,
        EndOperation,
        Retry,
        Reconnect,
        Subscribed
    }
}