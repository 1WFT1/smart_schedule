using System.Text.Json.Serialization;

namespace TopAcademyAPI.Journal.Endpoints.Settings.UserInfo.Models;

public record Visibility(
    [property: JsonPropertyName("is_design")] bool IsDesign,
    [property: JsonPropertyName("is_video_courses")] bool IsVideoCourses,
    [property: JsonPropertyName("is_vacancy")] bool IsVacancy,
    [property: JsonPropertyName("is_signal")] bool IsSignal,
    [property: JsonPropertyName("is_promo")] bool IsPromo,
    [property: JsonPropertyName("is_test")] bool IsTest,
    [property: JsonPropertyName("is_email_verified")] bool IsEmailVerified,
    [property: JsonPropertyName("is_quizzes_expired")] bool IsQuizzesExpired,
    [property: JsonPropertyName("is_debtor")] bool IsDebtor,
    [property: JsonPropertyName("is_phone_verified")] bool IsPhoneVerified,
    [property: JsonPropertyName("is_only_profile")] bool IsOnlyProfile,
    [property: JsonPropertyName("is_referral_program")] bool IsReferralProgram,
    [property: JsonPropertyName("is_dz_group_issue")] bool IsDzGroupIssue,
    [property: JsonPropertyName("is_birthday")] bool IsBirthday,
    [property: JsonPropertyName("is_school")] bool IsSchool,
    [property: JsonPropertyName("is_news_popup")] bool IsNewsPopup,
    [property: JsonPropertyName("is_school_branch")] bool IsSchoolBranch,
    [property: JsonPropertyName("is_college_branch")] bool IsCollegeBranch,
    [property: JsonPropertyName("is_higher_education_branch")] bool IsHigherEducationBranch,
    [property: JsonPropertyName("is_russian_branch")] bool IsRussianBranch);