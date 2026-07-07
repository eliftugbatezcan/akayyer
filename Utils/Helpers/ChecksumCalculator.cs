using System;
using System.Security.Cryptography;
using System.Text;

namespace GroundStation.Utils.Helpers
{
    /// <summary>
    /// Telemetri paketlerinin bütünlük kontrolünü (integrity check) yapmak üzere
    /// CRC16 ve MD5 algoritmalarını barındıran yardımcı sınıftır.
    /// </summary>
    public static class ChecksumCalculator
    {
        private const ushort Polinomial = 0x1021; // CCITT Polinomial
        private const ushort InitialValue = 0xFFFF;

        /// <summary>
        /// Belirtilen string verinin CRC16 (CCITT-False) değerini hesaplar.
        /// </summary>
        public static ushort ComputeCrc16(string data)
        {
            if (string.IsNullOrEmpty(data)) return 0;
            
            byte[] bytes = Encoding.ASCII.GetBytes(data);
            ushort crc = InitialValue;

            for (int i = 0; i < bytes.Length; i++)
            {
                crc ^= (ushort)(bytes[i] << 8);
                for (int j = 0; j < 8; j++)
                {
                    if ((crc & 0x8000) != 0)
                    {
                        crc = (ushort)((crc << 1) ^ Polinomial);
                    }
                    else
                    {
                        crc = (ushort)(crc << 1);
                    }
                }
            }

            return crc;
        }

        /// <summary>
        /// Belirtilen string verinin MD5 hash değerini hesaplayarak hexadecimal string döndürür.
        /// </summary>
        public static string ComputeMd5(string data)
        {
            if (string.IsNullOrEmpty(data)) return string.Empty;

            using (MD5 md5 = MD5.Create())
            {
                byte[] inputBytes = Encoding.ASCII.GetBytes(data);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("x2"));
                }
                return sb.ToString();
            }
        }
    }
}
