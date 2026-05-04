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
    DepartmentChanged = 30,
    UserChanged = 40,
    CsvExported = 50
}
