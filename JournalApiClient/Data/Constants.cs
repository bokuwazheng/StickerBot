namespace JournalApiClient.Data.Constants
{
    public class Comment
    {
        public const string LowQuality = "Low quality.";
        public const string DoesNotFit = "Does not fit.";
        public const string TooSimilar = "Too similar to an existing sticker.";
        public const string Other = "Personal preferences or other reasons.";
    }

    public class Reply
    {
        public const string ThankYou = "Thank you! To get notified when your submission status changes type '/subscribe'. You can also check the status manually using '/status id' command.";
        public const string WrongUpdateType = "Please send an image or enter a command.";
        public const string Subscribed = "You will receive a notification once your submission status is changed.";
        public const string Approved = "Your suggestion has been approved!";
        public const string Declined = "Your suggestion has been declined.";
    }
}
