// This file is subject to the terms and conditions defined
// in file 'LICENSE', which is part of this source code package.

using System;
using System.Threading.Tasks;
using SteamKit2.Authentication;

namespace DepotDownloader
{
    // This is practically copied from https://github.com/SteamRE/SteamKit/blob/master/SteamKit2/SteamKit2/Steam/Authentication/UserConsoleAuthenticator.cs
    internal class ConsoleAuthenticator : IAuthenticator
    {
        private const int MaxCodeAttempts = 3;

        /// <inheritdoc />
        public Task<string> GetDeviceCodeAsync(bool previousCodeWasIncorrect)
        {
            if (previousCodeWasIncorrect)
            {
                Console.Error.WriteLine("STEAM GUARD! The 2-factor auth code you entered was incorrect. Please try again.");
            }

            string code;
            var attempts = 0;

            do
            {
                if (attempts >= MaxCodeAttempts)
                {
                    Console.Error.WriteLine($"STEAM GUARD! Failed to provide a valid 2-factor auth code after {MaxCodeAttempts} attempts.");
                    return Task.FromResult<string>(null);
                }

                Console.Error.Write("STEAM GUARD! Please enter your 2-factor auth code from your authenticator app: ");
                code = Console.ReadLine()?.Trim();
                attempts++;

                if (code == null)
                {
                    break;
                }
            }
            while (string.IsNullOrEmpty(code));

            return Task.FromResult(code!);
        }

        /// <inheritdoc />
        public Task<string> GetEmailCodeAsync(string email, bool previousCodeWasIncorrect)
        {
            if (previousCodeWasIncorrect)
            {
                Console.Error.WriteLine($"STEAM GUARD! The auth code you entered was incorrect. A new code has been sent to {email}.");
            }

            string code;
            var attempts = 0;

            do
            {
                if (attempts >= MaxCodeAttempts)
                {
                    Console.Error.WriteLine($"STEAM GUARD! Failed to provide a valid email auth code after {MaxCodeAttempts} attempts.");
                    return Task.FromResult<string>(null);
                }

                Console.Error.Write($"STEAM GUARD! Please enter the auth code sent to the email at {email}: ");
                code = Console.ReadLine()?.Trim();
                attempts++;

                if (code == null)
                {
                    break;
                }
            }
            while (string.IsNullOrEmpty(code));

            return Task.FromResult(code!);
        }

        /// <inheritdoc />
        public Task<bool> AcceptDeviceConfirmationAsync()
        {
            if (ContentDownloader.Config.SkipAppConfirmation)
            {
                return Task.FromResult(false);
            }

            Console.Error.WriteLine("STEAM GUARD! Use the Steam Mobile App to confirm your sign in...");
            Console.Error.WriteLine("             (Use -no-mobile to enter a 2FA code instead.)");

            return Task.FromResult(true);
        }
    }
}
