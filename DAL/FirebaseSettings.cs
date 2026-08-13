using System;
using System.Collections.Specialized;

namespace DAL
{
    public static class FirebaseSettings
    {
        private const string URL_POR_DEFECTO = "https://smartdrop-60e34-default-rtdb.firebaseio.com/";

        public static string DatabaseUrlOverride { get; set; } = "";
        public static string AuthTokenOverride { get; set; } = "";

        public static string DatabaseUrl
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(DatabaseUrlOverride)) return DatabaseUrlOverride;

                var url = LeerAppSetting("Firebase:DatabaseUrl");
                if (!string.IsNullOrWhiteSpace(url)) return url;

                return URL_POR_DEFECTO;
            }
        }

        public static string AuthToken
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(AuthTokenOverride)) return AuthTokenOverride;
                return LeerAppSetting("Firebase:AuthToken") ?? "";
            }
        }

        // Lee AppSettings de App.config mediante reflexión para evitar
        // dependencia directa de System.Configuration (compatible .NET Framework y .NET Core)
        private static string LeerAppSetting(string clave)
        {
            try
            {
                var tipo = Type.GetType("System.Configuration.ConfigurationManager, System.Configuration, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");
                if (tipo == null)
                    tipo = Type.GetType("System.Configuration.ConfigurationManager");

                if (tipo == null) return null;

                var prop = tipo.GetProperty("AppSettings");
                if (prop == null) return null;

                var appSettings = prop.GetValue(null) as NameValueCollection;
                return appSettings?.Get(clave);
            }
            catch { return null; }
        }
    }
}