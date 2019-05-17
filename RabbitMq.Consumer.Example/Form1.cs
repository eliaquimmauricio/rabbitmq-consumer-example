using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace RabbitMq.Consumer.Example
{
    public partial class Form1 : Form, IDisposable
    {
        private IModel _channel;
        
        private readonly string _id = new Random().Next(100, 999).ToString();

        delegate void ReceveidMessageHandler(string msg);

        public Form1()
        {
            InitializeComponent();

            txtId.Text = _id;
        }

        private void btnConect_Click(object sender, EventArgs e)
        {
            string queue = txtQueueName.Text;

            //Criando o canal de comunicação com o RabbitMQ.

            _channel = new ConnectionFactory { HostName = txtServer.Text }.CreateConnection()
                                                                          .CreateModel();

            //Declarando a fila que será utilizada, caso não exista com esse nome ele irá criar automaticamente.

            _channel.QueueDeclare(queue, false, false, true, null);

            //Declarando um consumidor para rastrear todas as mensagem recebidas na fila.

            EventingBasicConsumer consumer = new EventingBasicConsumer(_channel);
            consumer.Received += ReceivedText;

            _channel.BasicConsume(consumer, queue, true);

            btnConect.Enabled = false;
        }

        private void ReceivedText(object model, BasicDeliverEventArgs ea)
        {
            string message = Encoding.UTF8.GetString(ea.Body);

            //Escrevendo a mensagem recebida na tela.

            AddMessage(message);

            //Criando delay ficticio para testar

            Thread.Sleep(10000);

            //Crio as propriedades de resposta e associo o id de correlação recebido com o que será enviado.

            IBasicProperties replyProps = _channel.CreateBasicProperties();
            replyProps.CorrelationId = ea.BasicProperties.CorrelationId;

            //Declarando a mensagem de resposta e enviando na mesma fila.

            byte[] responseBytes = Encoding.UTF8.GetBytes($"O consumer [{_id}] recebeu e processou sua mensagem enviada: \"{message}\"");

            _channel.BasicPublish(string.Empty, ea.BasicProperties.ReplyTo, replyProps, responseBytes);
        }

        private void AddMessage(string msg)
        {
            if (InvokeRequired)
            {
                ReceveidMessageHandler callback = AddMessage;
                Invoke(callback, msg);
                return;
            }

            txtReceveidText.Text += ">>> " + msg + Environment.NewLine;
        }

        private void Dispose()
        {
            _channel.Dispose();
            GC.SuppressFinalize(this);
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            Application.Exit();
        }
    }
}
