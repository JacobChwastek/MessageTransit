namespace MessageTransit.AzureTable
{
    using Azure.Data.Tables;


    public interface ICloudTableProvider<in TSaga>
        where TSaga : class, ISaga
    {
        TableClient GetCloudTable();
    }
}
