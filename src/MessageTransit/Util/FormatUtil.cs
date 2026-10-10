namespace MessageTransit.Util
{
    using NewIdFormatters;


    public static class FormatUtil
    {
        public static readonly INewIdFormatter Formatter = new ZBase32Formatter();
    }
}
