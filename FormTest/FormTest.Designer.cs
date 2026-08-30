namespace CheckTranslation.FormTest
{
    partial class FormTest
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormTest));
            button = new Button();
            checkBox = new CheckBox();
            label_A = new Label();
            label_B = new Label();
            label_Fixed = new Label();
            label1 = new Label();
            SuspendLayout();
            // 
            // button
            // 
            resources.ApplyResources(button, "button");
            button.Name = "button";
            button.UseVisualStyleBackColor = true;
            // 
            // checkBox
            // 
            resources.ApplyResources(checkBox, "checkBox");
            checkBox.Name = "checkBox";
            checkBox.UseVisualStyleBackColor = true;
            // 
            // label_A
            // 
            resources.ApplyResources(label_A, "label_A");
            label_A.Name = "label_A";
            // 
            // label_B
            // 
            resources.ApplyResources(label_B, "label_B");
            label_B.Name = "label_B";
            // 
            // label_Fixed
            // 
            resources.ApplyResources(label_Fixed, "label_Fixed");
            label_Fixed.Name = "label_Fixed";
            // 
            // label1
            // 
            resources.ApplyResources(label1, "label1");
            label1.Name = "label1";
            // 
            // FormTest
            // 
            resources.ApplyResources(this, "$this");
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(label1);
            Controls.Add(label_Fixed);
            Controls.Add(label_B);
            Controls.Add(label_A);
            Controls.Add(checkBox);
            Controls.Add(button);
            Name = "FormTest";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button button;
        private CheckBox checkBox;
        private Label label_A;
        private Label label_B;
        private Label label_Fixed;
        private Label label1;
    }
}