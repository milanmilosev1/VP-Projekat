namespace SmartGrid.Server.Events
{
    public delegate void TransferEventHandler(object sender, TransferEventArgs e);
    public delegate void SampleReceivedEventHandler(object sender, SampleReceivedEventArgs e);
    public delegate void WarningRaisedEventHandler(object sender, WarningRaisedEventArgs e);
}
