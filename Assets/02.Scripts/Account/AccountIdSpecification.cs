using System.Text.RegularExpressions;

public class AccountIdSpecification
{
    // ID(이메일) : 로컬@도메인.최상위도메인, '.'은 연속되거나 맨 앞/끝, '@' 앞뒤에 올 수 없음
    private static readonly Regex EmailRegex = new Regex(
        @"^[A-Za-z0-9_+\-]+(\.[A-Za-z0-9_+\-]+)*@[A-Za-z0-9\-]+(\.[A-Za-z0-9\-]+)*\.[A-Za-z]{2,}\z");

    public string ErrorMessage => "아이디는 이메일 형식이어야 합니다.";

    public bool IsSatisfiedBy(string email)
    {
        return EmailRegex.IsMatch(email);
    }
}