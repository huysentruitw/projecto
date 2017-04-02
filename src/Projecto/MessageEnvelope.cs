using System;

namespace Projecto
{
    /// <summary>
    /// Base class for the message envelope that can be used to send along additional out-of-band information with a message.
    /// </summary>
    public abstract class MessageEnvelope
    {
        /// <summary>
        /// Constructs a new <see cref="MessageEnvelope"/>.
        /// </summary>
        /// <param name="sequenceNumber">The sequence number of the message.</param>
        /// <param name="message">The message.</param>
        protected MessageEnvelope(int sequenceNumber, object message)
        {
            if (sequenceNumber < 1) throw new ArgumentOutOfRangeException(nameof(sequenceNumber), "The sequence number must be greater than or equal to 1");
            if (message == null) throw new ArgumentNullException(nameof(message));
            SequenceNumber = sequenceNumber;
            Message = message;
        }

        /// <summary>
        /// The sequence number of this message.
        /// </summary>
        public int SequenceNumber { get; private set; }

        /// <summary>
        /// The message that gets projected to the matching projection handlers.
        /// </summary>
        public object Message { get; private set; }
    }
}
