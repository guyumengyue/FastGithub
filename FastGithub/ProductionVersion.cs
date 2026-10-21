using System;
using System.Reflection;
using System.Diagnostics.CodeAnalysis;

namespace FastGithub
{
    /// <summary>
    /// 表示产品版本
    /// </summary>
    public class ProductionVersion : IComparable<ProductionVersion>
    {
        private static readonly string? productionVersion = Assembly
            .GetEntryAssembly()?
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion;

        /// <summary>
        /// 获取当前应用程序的产品版本
        /// </summary>
        public static ProductionVersion? Current { get; } = TryParse(productionVersion, out var current) ? current : null;


        /// <summary>
        /// 版本
        /// </summary>
        public Version Version { get; }

        /// <summary>
        /// 子版本
        /// </summary>
        public string SubVersion { get; }

        /// <summary>
        /// 产品版本
        /// </summary>
        /// <param name="version"></param>
        /// <param name="subVersion"></param>
        public ProductionVersion(Version version, string subVersion)
        {
            this.Version = version;
            this.SubVersion = subVersion;
        }

        /// <summary>
        /// 比较版本
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public int CompareTo(ProductionVersion? other)
        {
            var x = this;
            var y = other;

            if (y == null)
            {
                return 1;
            }

            var value = x.Version.CompareTo(y.Version);
            if (value == 0)
            {
                value = CompareSubVerson(x.SubVersion, y.SubVersion);
            }
            return value;

            static int CompareSubVerson(string subX, string subY)
            {
                if (subX.Length == 0 && subY.Length == 0)
                {
                    return 0;
                }
                if (subX.Length == 0)
                {
                    return 1;
                }
                if (subY.Length == 0)
                {
                    return -1;
                }

                return StringComparer.OrdinalIgnoreCase.Compare(subX, subY);
            }
        }

        public override string ToString()
        {
            return $"{Version}{SubVersion}";
        }

        /// <summary>
        /// 解析
        /// </summary>
        /// <param name="productionVersion"></param>
        /// <returns></returns>
        public static ProductionVersion Parse(string productionVersion)
        {
            if (TryParse(productionVersion, out var result))
            {
                return result;
            }
            throw new FormatException($"无效的产品版本：{productionVersion}");
        }

        /// <summary>
        /// 解析程序集版本。构建元数据不影响版本比较，预发布后缀单独保留。
        /// </summary>
        public static bool TryParse(string? value, [NotNullWhen(true)] out ProductionVersion? result)
        {
            result = null;
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            var versionText = value.Split('+', 2)[0];
            var parts = versionText.Split('-', 2);
            if (Version.TryParse(parts[0], out var version) == false ||
                (parts.Length == 2 && parts[1].Length == 0))
            {
                return false;
            }

            var subVersion = parts.Length == 2 ? $"-{parts[1]}" : string.Empty;
            result = new ProductionVersion(version, subVersion);
            return true;
        }
    }
}
