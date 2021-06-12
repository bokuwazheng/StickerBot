using System.Collections.Generic;
using System.Linq;

namespace JournalApiClient.Data.Constants
{
    public class Comment
    {
        public const string LowQuality = "Low quality.";
        public const string DoesNotFit = "Does not fit.";
        public const string TooSimilar = "Too similar to an existing sticker.";
        public const string Other = "Personal preferences or other reasons.";

        public static List<string> ToList() => typeof(Comment).GetFields().Select(x => (string)x.GetValue(null)).ToList();
    }

    public class Reply
    {
        public const string Hello = "Hey there! Before submitting please check out the guidelines. To submit just send me an uncopressed .jpg or .png file.";
        public const string ThankYou = "Thank you! To get notified when your submission status changes use '/subscribe' command. Alternatively you can check the status manually using '/status {0}' command.";
        public const string WrongUpdateType = "Please send an image as a file or enter a command.";
        public const string WrongCommand = "Please enter a valid command. Type '/' into your chat box to see the list of available commands.";
        public const string Subscribed = "You will receive a notification once your submission status is changed. Use '/unsubscribe' to cancel.";
        public const string AlreadySubscribed = "You are already subscribed to notifications. In case you wanted to unsubscribe use '/unsubscribe'.";
        public const string AlreadyUnsubscribed = "You are not currently subscribed to notifications. In case you wanted to subscribe use '/subscribe'.";
        public const string Unsubscribed = "You will no longer receive notifications. Use '/subscribe' to cancel.";
        public const string Approved = "Your suggestion has been approved!";
        public const string Declined = "Your suggestion has been declined.";
        public const string StatusUnavaliable = "Submission status unavaliable.";
        public const string SuggestionNotFound = "Coundn't find suggestion with id {0}.";
        public const string NotYetReviewed = "Suggestion {0} has not yet been reviewed.";
        public const string InvalidId = "{0} is not a valid id.";
        public const string NoSubmissionsOrNotReview = "Either no submissions or the latest submission is not yet reviewed.";
        public const string UseStatusN = "{0} Use '/status N' (where N is the id of your submission) to check the status of a specific submission.";
    }
}
