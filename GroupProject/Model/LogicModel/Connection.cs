namespace GroupProject.Model.LogicModel;

public class Connection
{ 
        public int SourceCardId { get; set; }
        public int TargetCardId { get; set; }
        public int TargetInputIndex { get; set; }
        public Microsoft.Maui.Controls.Shapes.Line LineShape { get; set; }
}