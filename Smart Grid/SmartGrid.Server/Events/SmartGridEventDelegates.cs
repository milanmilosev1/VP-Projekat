namespace SmartGrid.Server.Events
{
    public delegate void TransferEventHandler(object sender, TransferEventArgs e);
    public delegate void SampleReceivedEventHandler(object sender, SampleReceivedEventArgs e);
    public delegate void WarningRaisedEventHandler(object sender, WarningRaisedEventArgs e);
    public delegate void ValidationWarningEventHandler(object sender, ValidationWarningEventArgs e);
    public delegate void VoltageSpikeEventHandler(object sender, VoltageSpikeEventArgs e);
}
