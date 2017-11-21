namespace pacmanClient {
    partial class Form1 {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
			this.label1 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.tbMsg = new System.Windows.Forms.TextBox();
			this.tbChat = new System.Windows.Forms.TextBox();
			this.SuspendLayout();
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.label1.Location = new System.Drawing.Point(3, 3);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(76, 20);
			this.label1.TabIndex = 71;
			this.label1.Text = "Score: 0";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 20.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.label2.Location = new System.Drawing.Point(178, -1);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(0, 31);
			this.label2.TabIndex = 72;
			// 
			// tbMsg
			// 
			this.tbMsg.Enabled = false;
			this.tbMsg.Location = new System.Drawing.Point(367, 315);
			this.tbMsg.Name = "tbMsg";
			this.tbMsg.Size = new System.Drawing.Size(100, 20);
			this.tbMsg.TabIndex = 143;
			this.tbMsg.KeyDown += new System.Windows.Forms.KeyEventHandler(this.MessageSend);
			// 
			// tbChat
			// 
			this.tbChat.Enabled = false;
			this.tbChat.Location = new System.Drawing.Point(367, 40);
			this.tbChat.Multiline = true;
			this.tbChat.Name = "tbChat";
			this.tbChat.ScrollBars = System.Windows.Forms.ScrollBars.Horizontal;
			this.tbChat.Size = new System.Drawing.Size(100, 255);
			this.tbChat.TabIndex = 144;
			this.tbChat.WordWrap = false;
			// 
			// Form1
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(479, 344);
			this.Controls.Add(this.tbChat);
			this.Controls.Add(this.tbMsg);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.label1);
			this.Name = "Form1";
			this.Text = "DADman";
			this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.KeyIsDown);
			this.KeyUp += new System.Windows.Forms.KeyEventHandler(this.KeyIsUp);
			this.ResumeLayout(false);
			this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox tbMsg;
        private System.Windows.Forms.TextBox tbChat;
    }
}

