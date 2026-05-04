using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using codecrafters_redis.src.Redis;

namespace codecrafters_redis.src.Core
{
    public class RdbLoader
    {
        public static Dictionary<string, RedisValue> Load(string filePath)
        {
            var store = new Dictionary<string, RedisValue>();
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"RDB file not found: {filePath}");
                return store;
            }

            try
            {
                using var fs = File.OpenRead(filePath);
                using var reader = new BinaryReader(fs);

                // Magic string "REDIS"
                var magic = Encoding.ASCII.GetString(reader.ReadBytes(5));
                if (magic != "REDIS")
                {
                    Console.WriteLine("Invalid RDB file: Magic string mismatch");
                    return store;
                }

                // Version (4 bytes)
                var version = Encoding.ASCII.GetString(reader.ReadBytes(4));
                Console.WriteLine($"RDB Version: {version}");

                while (fs.Position < fs.Length)
                {
                    byte marker = reader.ReadByte();
                    
                    if (marker == 0xFF) break; // EOF

                    if (marker == 0xFA) // Attribute/Metadata
                    {
                        ReadString(reader); // key
                        ReadString(reader); // value
                        continue;
                    }

                    if (marker == 0xFE) // SELECTDB
                    {
                        ReadLength(reader); // DB index
                        continue;
                    }

                    if (marker == 0xFB) // RESIZEDB
                    {
                        ReadLength(reader); // DB hash table size
                        ReadLength(reader); // Expires hash table size
                        continue;
                    }

                    DateTime? expiresAt = null;
                    if (marker == 0xFD) // Expiry in seconds
                    {
                        uint seconds = reader.ReadUInt32(); // Little endian
                        expiresAt = DateTimeOffset.FromUnixTimeSeconds(seconds).UtcDateTime;
                        marker = reader.ReadByte(); // Read type
                    }
                    else if (marker == 0xFC) // Expiry in milliseconds
                    {
                        ulong ms = reader.ReadUInt64(); // Little endian
                        expiresAt = DateTimeOffset.FromUnixTimeMilliseconds((long)ms).UtcDateTime;
                        marker = reader.ReadByte(); // Read type
                    }

                    // marker is now the value type
                    if (marker == 0) // String
                    {
                        string key = ReadString(reader);
                        string value = ReadString(reader);
                        store[key] = new RedisString { type = value, ExpiresAt = expiresAt };
                    }
                    else
                    {
                        // Skip other types for now or handle them if needed
                        // For the current stage, we only care about keys.
                        // However, to correctly advance the pointer, we should ideally know how to skip.
                        // But since we only expect strings in this stage, we can just log and break or try to skip key/value.
                        string key = ReadString(reader);
                        // Skip value (this is hard without knowing the type's format)
                        // For now, let's just assume it's a string or we stop.
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading RDB: {ex.Message}");
            }

            return store;
        }

        private static string ReadString(BinaryReader reader)
        {
            var (length, isSpecial) = ReadLength(reader);
            if (isSpecial)
            {
                if (length == 0) return reader.ReadByte().ToString();
                if (length == 1) return reader.ReadUInt16().ToString();
                if (length == 2) return reader.ReadUInt32().ToString();
                return "";
            }
            return Encoding.UTF8.GetString(reader.ReadBytes((int)length));
        }

        private static (uint length, bool isSpecial) ReadLength(BinaryReader reader)
        {
            byte b = reader.ReadByte();
            int type = (b & 0xC0) >> 6;

            if (type == 0) return ((uint)(b & 0x3F), false);
            if (type == 1)
            {
                byte b2 = reader.ReadByte();
                return ((uint)(((b & 0x3F) << 8) | b2), false);
            }
            if (type == 2)
            {
                byte[] bytes = reader.ReadBytes(4);
                if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
                return (BitConverter.ToUInt32(bytes, 0), false);
            }
            if (type == 3) return ((uint)(b & 0x3F), true);

            return (0, false);
        }
    }
}
