namespace CareConnect.Domain.Enums;

public enum AuditAction
{
    LoginSucceeded = 1,
    LoginFailed = 2,
    Logout = 3,
    NoticeCreated = 10,
    NoticeUpdated = 11,
    NoticePublished = 12,
    NoticeArchived = 13,
    AcknowledgementSubmitted = 20,
    AcknowledgementVoided = 21,
    AcknowledgementCorrected = 22,
    DepartmentChanged = 30,
    StaffChanged = 31,
    UserChanged = 40,
    CsvExported = 50
}
