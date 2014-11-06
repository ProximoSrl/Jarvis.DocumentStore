using Jarvis.DocumentStore.Core.Model;
using Machine.Specifications;

namespace Jarvis.DocumentStore.Tests.DomainSpecs.DocumentSpecs
{
    // ReSharper disable InconsistentNaming
    [Subject(typeof(FileNameWithExtension))]
    public class when_a_filename_is_created
    {
        static FileNameWithExtension _fname;
        Because of = () => _fname = 
            new FileNameWithExtension("tEmp.TxT");
        
        It file_name_should_be_temp = () => 
            _fname.FileName.ShouldBeLike("tEmp");
        
        It extension_should_be_txt = () => 
            _fname.Extension.ShouldBeLike("txt");
    }
}
