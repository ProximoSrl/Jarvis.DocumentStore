using System;
using Jarvis.DocumentStore.Core.Domain.Document;
using Jarvis.DocumentStore.Core.Model;
using Machine.Specifications;

namespace Jarvis.DocumentStore.Tests.DomainSpecs
{
    [Subject("Document with an handle assigned twice")]
    public class when_deleting_an_handle : DocumentSpecifications
    {
        private static Exception Exception { get; set; }
        private Establish context = () =>
        {
            var state = new DocumentState();
            state.Handles.Add(new DocumentHandle("h"),2);
            SetUp(state);
        };

        Because of = () =>
        {
            Document.Delete(new DocumentHandle("h"));
        };

        It state_should_track_handle = () =>
        {
            State.Handles.Count.ShouldBeLike(1);
        };
    }
}