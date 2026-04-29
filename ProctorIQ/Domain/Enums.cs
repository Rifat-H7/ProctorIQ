namespace ProctorIQ.Domain;

public enum UserRole { Admin, Proctor, Candidate }
public enum ExamStatus { Draft, Scheduled, Live, Ended }
public enum AttemptStatus { InProgress, Submitted, Terminated, Expired }
public enum QuestionType { Mcq, TrueFalse }
public enum AttemptSessionStatus { Connected, Disconnected, Suspicious, Terminated, Submitted, Expired }
