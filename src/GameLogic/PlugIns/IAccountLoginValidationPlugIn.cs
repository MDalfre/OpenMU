namespace MUnique.OpenMU.GameLogic.PlugIns;

using System.ComponentModel;
using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.Views.Login;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// A plugin interface which validates an account before its game server session is established.
/// </summary>
[Guid("5926742E-7D7E-4A83-A1A9-4B35E8EF4447")]
[PlugInPoint("Account login validation", "Validates an account before its game server session is established.")]
public interface IAccountLoginValidationPlugIn
{
    /// <summary>
    /// Validates the account login request.
    /// </summary>
    /// <param name="player">The connecting player.</param>
    /// <param name="eventArgs">The validation arguments.</param>
    ValueTask ValidateAccountLoginAsync(Player player, AccountLoginValidationEventArgs eventArgs);
}

/// <summary>
/// Event arguments for account login validation.
/// </summary>
public class AccountLoginValidationEventArgs : CancelEventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AccountLoginValidationEventArgs"/> class.
    /// </summary>
    /// <param name="account">The account to validate.</param>
    public AccountLoginValidationEventArgs(Account account)
    {
        this.Account = account;
    }

    /// <summary>
    /// Gets the account to validate.
    /// </summary>
    public Account Account { get; }

    /// <summary>
    /// Gets or sets the result sent to the client when validation is cancelled.
    /// </summary>
    public LoginResult RejectionResult { get; set; } = LoginResult.NoChargeInfo;
}
