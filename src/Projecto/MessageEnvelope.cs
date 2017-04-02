namespace Projecto
{
    /// <summary>
    /// Base class for the message envelope that can be used to send along additional out-of-band information with a message.
    /// </summary>
    public class MessageEnvelope
    {
        /// <summary>
        /// The message that gets projected to the matching projection handlers.
        /// </summary>
        public object Message { get; protected set; }
    }
}
