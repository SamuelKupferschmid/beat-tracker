namespace BeatTracker.UI
{
    partial class MainForm
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
            this.btnStart = new System.Windows.Forms.Button();
            this.btnStop = new System.Windows.Forms.Button();
            this.lblBPM = new System.Windows.Forms.Label();
            this.grpAusgabe = new System.Windows.Forms.GroupBox();
            this.pnlCircle = new System.Windows.Forms.Panel();
            this.lblConfidence = new System.Windows.Forms.Label();
            this.grpSteuerung = new System.Windows.Forms.GroupBox();
            this.lblLaufzeit = new System.Windows.Forms.Label();
            this.grpAusgabe.SuspendLayout();
            this.grpSteuerung.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnStart
            // 
            this.btnStart.Location = new System.Drawing.Point(6, 19);
            this.btnStart.Name = "btnStart";
            this.btnStart.Size = new System.Drawing.Size(75, 23);
            this.btnStart.TabIndex = 0;
            this.btnStart.Text = "Start";
            this.btnStart.UseVisualStyleBackColor = true;
            // 
            // btnStop
            // 
            this.btnStop.Location = new System.Drawing.Point(87, 19);
            this.btnStop.Name = "btnStop";
            this.btnStop.Size = new System.Drawing.Size(75, 23);
            this.btnStop.TabIndex = 1;
            this.btnStop.Text = "Stop";
            this.btnStop.UseVisualStyleBackColor = true;
            // 
            // lblBPM
            // 
            this.lblBPM.AutoSize = true;
            this.lblBPM.Font = new System.Drawing.Font("Microsoft Sans Serif", 16F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblBPM.Location = new System.Drawing.Point(6, 16);
            this.lblBPM.Name = "lblBPM";
            this.lblBPM.Size = new System.Drawing.Size(156, 26);
            this.lblBPM.TabIndex = 2;
            this.lblBPM.Tag = "";
            this.lblBPM.Text = "Aktuelle BPM: ";
            // 
            // grpAusgabe
            // 
            this.grpAusgabe.Controls.Add(this.pnlCircle);
            this.grpAusgabe.Controls.Add(this.lblConfidence);
            this.grpAusgabe.Controls.Add(this.lblBPM);
            this.grpAusgabe.Location = new System.Drawing.Point(12, 69);
            this.grpAusgabe.Name = "grpAusgabe";
            this.grpAusgabe.Size = new System.Drawing.Size(407, 325);
            this.grpAusgabe.TabIndex = 3;
            this.grpAusgabe.TabStop = false;
            this.grpAusgabe.Text = "Ausgabe";
            // 
            // pnlCircle
            // 
            this.pnlCircle.Location = new System.Drawing.Point(13, 94);
            this.pnlCircle.Name = "pnlCircle";
            this.pnlCircle.Size = new System.Drawing.Size(388, 225);
            this.pnlCircle.TabIndex = 4;
            // 
            // lblConfidence
            // 
            this.lblConfidence.AutoSize = true;
            this.lblConfidence.Font = new System.Drawing.Font("Microsoft Sans Serif", 16F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblConfidence.Location = new System.Drawing.Point(6, 53);
            this.lblConfidence.Name = "lblConfidence";
            this.lblConfidence.Size = new System.Drawing.Size(121, 26);
            this.lblConfidence.TabIndex = 3;
            this.lblConfidence.Tag = "";
            this.lblConfidence.Text = "Konfidenz: ";
            // 
            // grpSteuerung
            // 
            this.grpSteuerung.Controls.Add(this.lblLaufzeit);
            this.grpSteuerung.Controls.Add(this.btnStart);
            this.grpSteuerung.Controls.Add(this.btnStop);
            this.grpSteuerung.Location = new System.Drawing.Point(12, 12);
            this.grpSteuerung.Name = "grpSteuerung";
            this.grpSteuerung.Size = new System.Drawing.Size(407, 51);
            this.grpSteuerung.TabIndex = 4;
            this.grpSteuerung.TabStop = false;
            this.grpSteuerung.Text = "Steuerung";
            // 
            // lblLaufzeit
            // 
            this.lblLaufzeit.AutoSize = true;
            this.lblLaufzeit.Location = new System.Drawing.Point(168, 24);
            this.lblLaufzeit.Name = "lblLaufzeit";
            this.lblLaufzeit.Size = new System.Drawing.Size(50, 13);
            this.lblLaufzeit.TabIndex = 2;
            this.lblLaufzeit.Tag = "Laufzeit: {0}";
            this.lblLaufzeit.Text = "Laufzeit: ";
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(429, 408);
            this.Controls.Add(this.grpSteuerung);
            this.Controls.Add(this.grpAusgabe);
            this.MaximizeBox = false;
            this.MaximumSize = new System.Drawing.Size(445, 447);
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(445, 447);
            this.Name = "MainForm";
            this.Text = "FHNW - IP6 FS19 - Real Time Beat Tracker";
            this.grpAusgabe.ResumeLayout(false);
            this.grpAusgabe.PerformLayout();
            this.grpSteuerung.ResumeLayout(false);
            this.grpSteuerung.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button btnStart;
        private System.Windows.Forms.Button btnStop;
        private System.Windows.Forms.Label lblBPM;
        private System.Windows.Forms.GroupBox grpAusgabe;
        private System.Windows.Forms.GroupBox grpSteuerung;
        private System.Windows.Forms.Label lblLaufzeit;
        private System.Windows.Forms.Label lblConfidence;
        private System.Windows.Forms.Panel pnlCircle;
    }
}