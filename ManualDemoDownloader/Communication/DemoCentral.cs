using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using RabbitCommunicationLib.Interfaces;
using RabbitCommunicationLib.Producer;
using RabbitCommunicationLib.TransferModels;

namespace ManualUpload.Communication
{
    public interface IDemoCentral :IHostedService
    {
        public void PublishMessage(string correlationId, GathererTransferModel produceModel);
    }

    public class DemoCentral : Producer<GathererTransferModel>, IDemoCentral
    {
        public DemoCentral(IQueueConnection queueConnection, bool persistentMessageSending = true) : base(queueConnection, persistentMessageSending)
        {
        }
    }
}
