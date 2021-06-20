namespace JournalApiClient.Data.Constants
{
    public class Reply
    {
        #region Intoduction
        public const string Hello = "Hey there!\n\nTo submit, just send me an uncompressed .jpg or .png file.";
        public const string BeforeSubmitting = "Before submitting, please see the guidelines.\n\n{0}";
        public const string ThankYou = "Thank you!\n\nTo get notified when your submission status changes use '/subscribe' command. Alternatively you can check the status manually using '/status {0}' command.";
        public const string Guidelines = "Please see the information by clicking the link below or tapping 'Instant View'.\n\n{0}"; // %0A
        #endregion

        #region Error
        public const string WrongUpdateType = "I'm sorry I don't quite understand what you are saying.\n\nPlease send an image as a file or enter a command (type '/' to view the list of commands).";
        public const string WrongCommand = "Please enter a valid command. Type '/' into your chat box or click (tap) '/' button to see the list of available commands.";
        #endregion

        #region Subscribe/unsubscribe command
        public const string Subscribed = "You will receive a notification once your submission status is changed. Please use '/unsubscribe' command to cancel.";
        public const string AlreadySubscribed = "You are already subscribed to notifications. In case you wanted to unsubscribe please use '/unsubscribe' command.";
        public const string AlreadyUnsubscribed = "You are currently not subscribed to notifications. In case you wanted to subscribe please use '/subscribe' command.";
        public const string Unsubscribed = "You will no longer receive notifications. Please use '/subscribe' command to cancel.";
        #endregion

        #region Status command
        public const string StatusUnavaliable = "Submission status unavaliable.";
        public const string SuggestionNotFound = "Coundn't find suggestion with ID {0}.";
        public const string NotYetReviewed = "Seems like this submission has not yet been reviewed.";
        public const string LatestNotYetReviewed = "Seems like your latest submission has not yet been reviewed.";
        public const string InvalidId = "{0} is not a valid ID.";
        public const string UseStatusN = "{0}\n\nUse '/status N' (N being the ID of your submission) command to check the status of a specific submission.";
        #endregion

        #region Notification
        public const string Approved = "Congratulations! I am pleased to inform you that, your submission has been approved! Most probably it'll make it to the pack. Fingers crossed!";
        public const string Declined = "Unfortunatelly, your suggestion has been declined. Stated reason: {0}.";
        #endregion

        public const string Banned = "Yikes! Seems like you got banned. You must have sent me some inappropriate stuff...";
    }
}
