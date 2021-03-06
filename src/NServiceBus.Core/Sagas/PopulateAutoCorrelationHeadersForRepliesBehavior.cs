﻿namespace NServiceBus
{
    using System;
    using NServiceBus.Sagas;
    using Pipeline;
    using Pipeline.Contexts;
    using Unicast;
    using Unicast.Transport;


    class PopulateAutoCorrelationHeadersForRepliesBehavior : IBehavior<OutgoingContext>
    {
        public void Invoke(OutgoingContext context, Action next)
        {
            if (context.OutgoingLogicalMessage.IsControlMessage())
            {
                next();
                return;
            }

            ActiveSagaInstance saga;

            if (context.TryGet(out saga) && !saga.NotFound)
            {
                context.OutgoingLogicalMessage.Headers[Headers.OriginatingSagaId] = saga.SagaId;
                context.OutgoingLogicalMessage.Headers[Headers.OriginatingSagaType] = saga.Metadata.SagaType.AssemblyQualifiedName;
            }

            //auto correlate with the saga we are replying to if needed
            if (context.DeliveryOptions is ReplyOptions  && context.IncomingMessage != null)
            {
                string sagaId;
                string sagaType;

                if (context.IncomingMessage.Headers.TryGetValue(Headers.OriginatingSagaId, out sagaId))
                {
                    context.OutgoingLogicalMessage.Headers[Headers.SagaId] = sagaId;
                }

                if (context.IncomingMessage.Headers.TryGetValue(Headers.OriginatingSagaType, out sagaType))
                {
                    context.OutgoingLogicalMessage.Headers[Headers.SagaType] = sagaType;
                }
            }

            next();
        }
    }
}