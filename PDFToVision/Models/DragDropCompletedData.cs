using System.Collections.Generic;

using Windows.ApplicationModel.DataTransfer;

namespace PDFToVision.Models
{
    public class DragDropCompletedData
    {
        public DataPackageOperation DropResult { get; set; }

        public IReadOnlyList<object> Items { get; set; }
    }
}
