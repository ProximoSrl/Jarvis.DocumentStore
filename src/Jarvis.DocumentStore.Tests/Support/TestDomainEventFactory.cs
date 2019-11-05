﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fasterflect;
using Jarvis.Framework.Shared.Events;
using Jarvis.NEventStoreEx.CommonDomainEx;

namespace Jarvis.DocumentStore.Tests.Support
{
    public static class DomainEventTestExtensions
    {
        public static T AssignIdForTest<T>(this T evt, IIdentity id) where T : DomainEvent
        {
            evt.SetPropertyValue("AggregateId", id);
            return evt;
        }

        public static T AssignIssuedByForTest<T>(this T evt, String issuedBy) where T : DomainEvent
        {
            if (evt.Context == null)
                evt.SetPropertyValue("Context", new Dictionary<String, Object>());

            evt.Context.Add("user.id", issuedBy);
            return evt;
        }
    }
}
