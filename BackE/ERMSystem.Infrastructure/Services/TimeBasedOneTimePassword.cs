using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace ERMSystem.Infrastructure.Services
{
    internal static class TimeBasedOneTimePassword
    {
        private const string Base32Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

        public static string GenerateSecretKey(int length = 20)
        {
            var bytes = RandomNumberGenerator.GetBytes(length);
            return Base32Encode(bytes);
        }

        public static bool VerifyCode(
            string secretKey,
            string code,
            DateTime utcNow,
            int timeStepSeconds,
            int digits,
            int allowedDriftWindows)
        {
            if (string.IsNullOrWhiteSpace(secretKey) || string.IsNullOrWhiteSpace(code))
            {
                return false;
            }

            var normalizedCode = NormalizeCode(code);
            if (normalizedCode.Length != digits)
            {
                return false;
            }

            var secretBytes = Base32Decode(secretKey);
            var currentCounter = GetCounter(utcNow, timeStepSeconds);

            for (var offset = -allowedDriftWindows; offset <= allowedDriftWindows; offset++)
            {
                var expected = ComputeCode(secretBytes, currentCounter + offset, digits);
                if (CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(expected),
                    Encoding.UTF8.GetBytes(normalizedCode)))
                {
                    return true;
                }
            }

            return false;
        }

        public static string BuildOtpAuthUri(string issuer, string username, string secretKey, int digits, int periodSeconds)
        {
            var safeIssuer = Uri.EscapeDataString(issuer.Trim());
            var label = Uri.EscapeDataString($"{issuer.Trim()}:{username.Trim()}");
            return $"otpauth://totp/{label}?secret={secretKey}&issuer={safeIssuer}&digits={digits.ToString(CultureInfo.InvariantCulture)}&period={periodSeconds.ToString(CultureInfo.InvariantCulture)}";
        }

        private static long GetCounter(DateTime utcNow, int timeStepSeconds)
        {
            var unixSeconds = new DateTimeOffset(utcNow).ToUnixTimeSeconds();
            return unixSeconds / timeStepSeconds;
        }

        private static string ComputeCode(byte[] secretBytes, long counter, int digits)
        {
            Span<byte> counterBytes = stackalloc byte[8];
            for (var i = 7; i >= 0; i--)
            {
                counterBytes[i] = (byte)(counter & 0xff);
                counter >>= 8;
            }

            using var hmac = new HMACSHA1(secretBytes);
            var hash = hmac.ComputeHash(counterBytes.ToArray());
            var offset = hash[^1] & 0x0f;
            var binaryCode =
                ((hash[offset] & 0x7f) << 24)
                | ((hash[offset + 1] & 0xff) << 16)
                | ((hash[offset + 2] & 0xff) << 8)
                | (hash[offset + 3] & 0xff);

            var divisor = (int)Math.Pow(10, digits);
            var otp = binaryCode % divisor;
            return otp.ToString(CultureInfo.InvariantCulture).PadLeft(digits, '0');
        }

        private static string Base32Encode(byte[] data)
        {
            var output = new StringBuilder((int)Math.Ceiling(data.Length / 5d) * 8);
            var bitBuffer = 0;
            var bitsInBuffer = 0;

            foreach (var b in data)
            {
                bitBuffer = (bitBuffer << 8) | b;
                bitsInBuffer += 8;

                while (bitsInBuffer >= 5)
                {
                    var index = (bitBuffer >> (bitsInBuffer - 5)) & 0x1f;
                    bitsInBuffer -= 5;
                    output.Append(Base32Alphabet[index]);
                }
            }

            if (bitsInBuffer > 0)
            {
                var index = (bitBuffer << (5 - bitsInBuffer)) & 0x1f;
                output.Append(Base32Alphabet[index]);
            }

            return output.ToString();
        }

        private static byte[] Base32Decode(string input)
        {
            var cleaned = input.Trim().TrimEnd('=').Replace(" ", string.Empty, StringComparison.Ordinal).ToUpperInvariant();
            var output = new List<byte>(cleaned.Length * 5 / 8);
            var bitBuffer = 0;
            var bitsInBuffer = 0;

            foreach (var c in cleaned)
            {
                var index = Base32Alphabet.IndexOf(c);
                if (index < 0)
                {
                    throw new InvalidOperationException("Invalid Base32 secret.");
                }

                bitBuffer = (bitBuffer << 5) | index;
                bitsInBuffer += 5;

                if (bitsInBuffer < 8)
                {
                    continue;
                }

                bitsInBuffer -= 8;
                output.Add((byte)((bitBuffer >> bitsInBuffer) & 0xff));
            }

            return output.ToArray();
        }

        private static string NormalizeCode(string code)
            => code.Trim().Replace(" ", string.Empty, StringComparison.Ordinal).Replace("-", string.Empty, StringComparison.Ordinal);
    }
}
