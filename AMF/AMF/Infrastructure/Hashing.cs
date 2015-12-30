using System.Text;
using System.Security.Cryptography;
using System.Collections.Generic;
using System;

namespace AMF.Infrastructure
{
    public class Hashing
    {
        private const int _saltSize = 32;
        private Encoding _utf = Encoding.UTF8;

        public HashData HashPass(string Pass, string Salt)
        {
            string _salt = string.IsNullOrEmpty(Salt) ? GenerateSalt() : Salt;
            string _adjusted = _salt + Pass + _salt;
            SHA512Managed _hasher = new SHA512Managed();
            byte[] _passBytes = _utf.GetBytes(_adjusted);
            StringBuilder _passBuilder = new StringBuilder();
            byte[] _hash = _hasher.ComputeHash(_passBytes);

            foreach (byte sub in _hash)
                _passBuilder.Append(sub.ToString("x2"));

            return new HashData { PassHash = _passBuilder.ToString(), Salt = _salt };
        }

        private string GenerateSalt()
        {
            var _crypto = new RNGCryptoServiceProvider();
            byte[] salt = new byte[_saltSize];
            _crypto.GetNonZeroBytes(salt);
            return Convert.ToBase64String(salt);
        }
    }
}