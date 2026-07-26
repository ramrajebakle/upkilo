namespace Upkilo.Core.Enums;

public enum WorkflowTriggerType
{
    BookingCreated, BookingCancelled, BookingCompleted, BookingNoShow, BookingRescheduled,
    ClientCreated, ClientUpdated, ClientTagAdded,
    PaymentReceived, PaymentFailed, RefundIssued,
    StaffCreated, StaffScheduleChanged,
    ReviewSubmitted, FormSubmitted,
    ManualTrigger
}

public enum WorkflowActionType
{
    SendEmail, SendSms, SendPushNotification,
    CreateTask, UpdateBookingStatus, UpdateClientTags, AddClientNote,
    Delay, WaitUntil,
    CallWebhook, RequestReview, IssueRefund,
    AssignStaff, RemoveStaff, ChargeCreditCard, SubWorkflow,
    ConditionBranch, EndWorkflow
}
