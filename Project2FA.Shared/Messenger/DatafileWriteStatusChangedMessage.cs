using CommunityToolkit.Mvvm.Messaging.Messages;
using System;

namespace Project2FA.Core.Messenger
{
    public class DatafileWriteStatusChangedMessage : ValueChangedMessage<bool>
    {
        public DatafileWriteStatusChangedMessage(bool status) : base(status)
        {

        }
    }
}
