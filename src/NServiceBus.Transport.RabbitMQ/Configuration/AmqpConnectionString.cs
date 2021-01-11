using System;

namespace NServiceBus.Transport.RabbitMQ
{
    class AmqpConnectionString
    {
        public string Host { get; }
        public int Port { get; }
        public bool UseTls { get; }
        public string UserName { get; }
        public string Password { get; }
        public string VHost { get; }

        AmqpConnectionString(string host, int port, bool useTls, string userName, string password, string vhost)
        {
            Host = host;
            Port = port;
            UseTls = useTls;
            UserName = userName;
            Password = password;
            VHost = vhost;
        }

        public static AmqpConnectionString Parse(string connectionString)
        {
            var uri = new Uri(connectionString);

            var useTls = string.Equals("amqps", uri.Scheme, StringComparison.OrdinalIgnoreCase);
            string userName = null;
            string password = null;
            string vhost = null;

            if (!string.IsNullOrEmpty(uri.UserInfo))
            {
                var userPass = uri.UserInfo.Split(':');
                if (userPass.Length > 2)
                {
                    throw new Exception($"Invalid user information: {uri.UserInfo}. Expected user and password separated by a colon.");
                }

                userName = UriDecode(userPass[0]);
                if (userPass.Length == 2)
                {
                    password = UriDecode(userPass[1]);
                }
            }

            if (uri.Segments.Length > 2)
            {
                throw new Exception($"Multiple segments are not allowed in the path of an AMQP URI: {string.Join(", ", uri.Segments)}");
            }

            if (uri.Segments.Length == 2)
            {
                vhost = UriDecode(uri.Segments[1]);
            }

            return new AmqpConnectionString(uri.Host, uri.Port, useTls, userName, password, vhost);
        }

        static string UriDecode(string value)
        {
            return Uri.UnescapeDataString(value);
        }
    }
}