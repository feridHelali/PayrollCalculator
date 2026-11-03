namespace PayrollCalculator.Application.Users;

/// <summary>Who is signed in. Holds no password or hash.</summary>
public sealed record UserSession(int UserId, string UserName, string DisplayName);

/// <summary>The application-wide session (registered as a singleton). Anonymous until a sign-in succeeds.</summary>
public sealed class CurrentUser
{
    public UserSession? Session { get; private set; }

    public bool IsAuthenticated => Session is not null;

    /// <summary>Raised after every sign-in and sign-out.</summary>
    public event EventHandler? Changed;

    public void SignIn(UserSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        Session = session;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void SignOut()
    {
        Session = null;
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
