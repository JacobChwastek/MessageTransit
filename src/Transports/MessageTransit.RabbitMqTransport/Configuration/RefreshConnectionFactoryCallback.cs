namespace MessageTransit;

using System.Threading.Tasks;
using RabbitMQ.Client;


public delegate Task RefreshConnectionFactoryCallback(ConnectionFactory connectionFactory);
