using System;
using System.Collections.Generic;
using System.Windows.Forms;
using ISCSIApp.Models;
using ISCSIApp.Services;

namespace ISCSIApp
{
    public partial class MainForm : Form
    {
        private readonly ISCSIService _iscsiService;
        private List<ISCSITarget> _discoveredTargets;
        private List<ISCSITarget> _connectedTargets;

        public MainForm()
        {
            InitializeComponent();
            _iscsiService = new ISCSIService();
            _discoveredTargets = new List<ISCSITarget>();
            _connectedTargets = new List<ISCSITarget>();
            LoadConnectedTargets();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // MainForm
            // 
            this.ClientSize = new System.Drawing.Size(800, 600);
            this.Name = "MainForm";
            this.Text = "Windows iSCSI Initiator";
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.ResumeLayout(false);

            // Create UI components
            CreateUI();
        }

        private void CreateUI()
        {
            // Tabs for different sections
            TabControl tabControl = new TabControl();
            tabControl.Dock = DockStyle.Fill;

            // Targets Tab
            TabPage targetsTab = new TabPage("Targets");
            CreateTargetsTab(targetsTab);
            tabControl.TabPages.Add(targetsTab);

            // Connected Tab
            TabPage connectedTab = new TabPage("Connected");
            CreateConnectedTab(connectedTab);
            tabControl.TabPages.Add(connectedTab);

            // Multi-Path I/O Tab
            TabPage mpioTab = new TabPage("Multi-Path I/O");
            CreateMPIOTab(mpioTab);
            tabControl.TabPages.Add(mpioTab);

            // Target Creation Tab
            TabPage createTargetTab = new TabPage("Create Target");
            CreateTargetCreationTab(createTargetTab);
            tabControl.TabPages.Add(createTargetTab);

            // Settings Tab
            TabPage settingsTab = new TabPage("Settings");
            CreateSettingsTab(settingsTab);
            tabControl.TabPages.Add(settingsTab);

            this.Controls.Add(tabControl);
        }

        private void CreateTargetsTab(TabPage tab)
        {
            // Portal input section
            Label portalLabel = new Label();
            portalLabel.Text = "Target Portal:";
            portalLabel.Location = new System.Drawing.Point(10, 15);
            portalLabel.AutoSize = true;

            TextBox portalTextBox = new TextBox();
            portalTextBox.Location = new System.Drawing.Point(110, 12);
            portalTextBox.Size = new System.Drawing.Size(200, 23);
            portalTextBox.Name = "portalTextBox";

            Button discoverButton = new Button();
            discoverButton.Text = "Discover";
            discoverButton.Location = new System.Drawing.Point(320, 10);
            discoverButton.Click += new EventHandler(DiscoverButton_Click);

            // Targets list view
            ListView targetsListView = new ListView();
            targetsListView.Location = new System.Drawing.Point(10, 50);
            targetsListView.Size = new System.Drawing.Size(760, 400);
            targetsListView.View = View.Details;
            targetsListView.FullRowSelect = true;
            targetsListView.Name = "targetsListView";
            targetsListView.Columns.Add("Target Name", 300);
            targetsListView.Columns.Add("Portal Address", 150);
            targetsListView.Columns.Add("Port", 80);
            targetsListView.Columns.Add("Status", 100);

            // Connect button
            Button connectButton = new Button();
            connectButton.Text = "Connect";
            connectButton.Location = new System.Drawing.Point(10, 460);
            connectButton.Click += new EventHandler(ConnectButton_Click);

            // Add controls to tab
            tab.Controls.Add(portalLabel);
            tab.Controls.Add(portalTextBox);
            tab.Controls.Add(discoverButton);
            tab.Controls.Add(targetsListView);
            tab.Controls.Add(connectButton);
        }

        private void CreateConnectedTab(TabPage tab)
        {
            // Connected targets list view
            ListView connectedListView = new ListView();
            connectedListView.Location = new System.Drawing.Point(10, 10);
            connectedListView.Size = new System.Drawing.Size(760, 440);
            connectedListView.View = View.Details;
            connectedListView.FullRowSelect = true;
            connectedListView.Name = "connectedListView";
            connectedListView.Columns.Add("Target Name", 300);
            connectedListView.Columns.Add("Portal Address", 150);
            connectedListView.Columns.Add("Port", 80);
            connectedListView.Columns.Add("Session ID", 100);

            // Disconnect button
            Button disconnectButton = new Button();
            disconnectButton.Text = "Disconnect";
            disconnectButton.Location = new System.Drawing.Point(10, 460);
            disconnectButton.Click += new EventHandler(DisconnectButton_Click);

            // Refresh button
            Button refreshButton = new Button();
            refreshButton.Text = "Refresh";
            refreshButton.Location = new System.Drawing.Point(110, 460);
            refreshButton.Click += new EventHandler(RefreshButton_Click);

            // Add controls to tab
            tab.Controls.Add(connectedListView);
            tab.Controls.Add(disconnectButton);
            tab.Controls.Add(refreshButton);
        }

        private void CreateSettingsTab(TabPage tab)
        {
            // Initiator settings
            Label initiatorNameLabel = new Label();
            initiatorNameLabel.Text = "Initiator Name:";
            initiatorNameLabel.Location = new System.Drawing.Point(10, 15);
            initiatorNameLabel.AutoSize = true;

            TextBox initiatorNameTextBox = new TextBox();
            initiatorNameTextBox.Location = new System.Drawing.Point(110, 12);
            initiatorNameTextBox.Size = new System.Drawing.Size(300, 23);
            initiatorNameTextBox.Name = "initiatorNameTextBox";
            initiatorNameTextBox.ReadOnly = true;

            // CHAP Authentication settings
            CheckBox chapCheckBox = new CheckBox();
            chapCheckBox.Text = "Enable CHAP Authentication";
            chapCheckBox.Location = new System.Drawing.Point(10, 50);
            chapCheckBox.AutoSize = true;
            chapCheckBox.Name = "chapCheckBox";

            Label usernameLabel = new Label();
            usernameLabel.Text = "Username:";
            usernameLabel.Location = new System.Drawing.Point(30, 80);
            usernameLabel.AutoSize = true;

            TextBox usernameTextBox = new TextBox();
            usernameTextBox.Location = new System.Drawing.Point(110, 77);
            usernameTextBox.Size = new System.Drawing.Size(200, 23);
            usernameTextBox.Name = "usernameTextBox";
            usernameTextBox.Enabled = false;

            Label passwordLabel = new Label();
            passwordLabel.Text = "Password:";
            passwordLabel.Location = new System.Drawing.Point(30, 110);
            passwordLabel.AutoSize = true;

            TextBox passwordTextBox = new TextBox();
            passwordTextBox.Location = new System.Drawing.Point(110, 107);
            passwordTextBox.Size = new System.Drawing.Size(200, 23);
            passwordTextBox.Name = "passwordTextBox";
            passwordTextBox.PasswordChar = '*';
            passwordTextBox.Enabled = false;

            chapCheckBox.CheckedChanged += (sender, e) => {
                usernameTextBox.Enabled = chapCheckBox.Checked;
                passwordTextBox.Enabled = chapCheckBox.Checked;
            };

            // Save settings button
            Button saveButton = new Button();
            saveButton.Text = "Save Settings";
            saveButton.Location = new System.Drawing.Point(10, 150);
            saveButton.Click += new EventHandler(SaveSettings_Click);

            // Add controls to tab
            tab.Controls.Add(initiatorNameLabel);
            tab.Controls.Add(initiatorNameTextBox);
            tab.Controls.Add(chapCheckBox);
            tab.Controls.Add(usernameLabel);
            tab.Controls.Add(usernameTextBox);
            tab.Controls.Add(passwordLabel);
            tab.Controls.Add(passwordTextBox);
            tab.Controls.Add(saveButton);
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            try
            {
                // Load initiator name
                TextBox initiatorNameTextBox = (TextBox)Controls.Find("initiatorNameTextBox", true)[0];
                initiatorNameTextBox.Text = _iscsiService.GetInitiatorName();
                
                // Load local targets
                LoadLocalTargets();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading initiator settings: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private void LoadLocalTargets()
        {
            try
            {
                // Get local targets
                List<ISCSITarget> localTargets = _iscsiService.GetLocalTargets();

                // Update list view
                ListView localTargetsListView = (ListView)Controls.Find("localTargetsListView", true)[0];
                if (localTargetsListView != null)
                {
                    localTargetsListView.Items.Clear();

                    foreach (var target in localTargets)
                    {
                        ListViewItem item = new ListViewItem(target.TargetName);
                        item.SubItems.Add(target.BackingStoragePath);
                        item.SubItems.Add($"{target.SizeGB} GB");
                        item.Tag = target;
                        localTargetsListView.Items.Add(item);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading local targets: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DiscoverButton_Click(object sender, EventArgs e)
        {
            string portalAddress = portalAddressTextBox.Text.Trim();
            if (string.IsNullOrEmpty(portalAddress))
            {
                MessageBox.Show("Please enter a portal address.", "Input Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            discoveredTargetsListView.Items.Clear();
            statusLabel.Text = "Discovering targets...";
            this.Cursor = Cursors.WaitCursor;

            try
            {
                _discoveredTargets = await _iscsiService.DiscoverTargetsAsync(portalAddress);
                foreach (var target in _discoveredTargets)
                {
                    var item = new ListViewItem(target.TargetName);
                    item.SubItems.Add(target.PortalAddress);
                    item.SubItems.Add(target.PortalPort.ToString());
                    item.Tag = target;
                    discoveredTargetsListView.Items.Add(item);
                }
                statusLabel.Text = $"Discovery complete. Found {_discoveredTargets.Count} targets.";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error discovering targets: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                statusLabel.Text = "Discovery failed.";
            }
            finally
            {
                this.Cursor = Cursors.Default;
            }
        }

        private async void ConnectButton_Click(object sender, EventArgs e)
        {
            if (discoveredTargetsListView.SelectedItems.Count == 0 && connectedTargetsListView.SelectedItems.Count == 0)
            {
                MessageBox.Show("Please select a target to connect.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            ISCSITarget target = null;
            if (discoveredTargetsListView.SelectedItems.Count > 0)
            {
                target = (ISCSITarget)discoveredTargetsListView.SelectedItems[0].Tag;
            }
            else if (connectedTargetsListView.SelectedItems.Count > 0)
            {
                target = (ISCSITarget)connectedTargetsListView.SelectedItems[0].Tag;
                if (target.IsConnected)
                {
                    MessageBox.Show("Target is already connected.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
            }

            if (target == null) return; // Should not happen

            bool isPersistent = persistentCheckBox.Checked;
            string username = chapUserTextBox.Text.Trim();
            string password = chapPasswordTextBox.Text;

            statusLabel.Text = $"Connecting to {target.TargetName}...";
            this.Cursor = Cursors.WaitCursor;

            try
            {
                bool success = await _iscsiService.ConnectToTargetAsync(target, isPersistent, 
                    string.IsNullOrEmpty(username) ? null : username, 
                    string.IsNullOrEmpty(password) ? null : password);

                if (success)
                {
                    statusLabel.Text = $"Successfully connected to {target.TargetName}.";
                    RefreshConnectedTargetsList(); // Refresh the list to show the new connection status
                    // Optionally move from discovered to connected list view if applicable
                }
                else
                {
                    MessageBox.Show("Failed to connect to the target.", "Connection Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    statusLabel.Text = "Connection failed.";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error connecting to target: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                statusLabel.Text = "Connection error.";
            }
            finally
            {
                this.Cursor = Cursors.Default;
            }
        }

        private async void DisconnectButton_Click(object sender, EventArgs e)
        {
            if (connectedTargetsListView.SelectedItems.Count == 0)
            {
                MessageBox.Show("Please select a connected target to disconnect.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            ISCSITarget target = (ISCSITarget)connectedTargetsListView.SelectedItems[0].Tag;

            if (!target.IsConnected)
            {
                MessageBox.Show("Target is not connected.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            statusLabel.Text = $"Disconnecting from {target.TargetName}...";
            this.Cursor = Cursors.WaitCursor;

            try
            {
                bool success = await _iscsiService.DisconnectFromTargetAsync(target);
                if (success)
                {
                    statusLabel.Text = $"Successfully disconnected from {target.TargetName}.";
                    RefreshConnectedTargetsList(); // Refresh the list
                }
                else
                {
                    MessageBox.Show("Failed to disconnect from the target.", "Disconnection Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    statusLabel.Text = "Disconnection failed.";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error disconnecting from target: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                statusLabel.Text = "Disconnection error.";
            }
            finally
            {
                this.Cursor = Cursors.Default;
            }
        }

        private void RefreshButton_Click(object sender, EventArgs e)
        {
            RefreshConnectedTargetsList();
        }

        private void SaveSettings_Click(object sender, EventArgs e)
        {
            try
            {
                // Save CHAP settings
                CheckBox chapCheckBox = (CheckBox)Controls.Find("chapCheckBox", true)[0];
                TextBox usernameTextBox = (TextBox)Controls.Find("usernameTextBox", true)[0];
                TextBox passwordTextBox = (TextBox)Controls.Find("passwordTextBox", true)[0];

                // Save settings to configuration
                // In a real application, you would save these settings securely
                MessageBox.Show("Settings saved successfully", "Settings Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving settings: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Erro        }
        
        #region MPIO Tab Event Handlers
        
        private void AddPath_Click(object sender, EventArgs e)
        {
            try
            {
                ListView mpioListView = (ListView)Controls.Find("mpioListView", true)[0];
                if (mpioListView.SelectedItems.Count == 0)
                {
                    MessageBox.Show("Please select a target to add a path", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                ISCSITarget selectedTarget = (ISCSITarget)mpioListView.SelectedItems[0].Tag;
                
                // Show dialog to get path details
                using (Form addPathDialog = new Form())
                {
                    addPathDialog.Text = "Add Path";
                    addPathDialog.Size = new System.Drawing.Size(350, 200);
                    addPathDialog.FormBorderStyle = FormBorderStyle.FixedDialog;
                    addPathDialog.StartPosition = FormStartPosition.CenterParent;
                    addPathDialog.MaximizeBox = false;
                    addPathDialog.MinimizeBox = false;

                    Label addressLabel = new Label();
                    addressLabel.Text = "Path Address:";
                    addressLabel.Location = new System.Drawing.Point(20, 20);
                    addressLabel.AutoSize = true;

                    TextBox addressTextBox = new TextBox();
                    addressTextBox.Location = new System.Drawing.Point(120, 17);
                    addressTextBox.Size = new System.Drawing.Size(200, 23);

                    Label portLabel = new Label();
                    portLabel.Text = "Port:";
                    portLabel.Location = new System.Drawing.Point(20, 50);
                    portLabel.AutoSize = true;

                    NumericUpDown portNumeric = new NumericUpDown();
                    portNumeric.Location = new System.Drawing.Point(120, 47);
                    portNumeric.Size = new System.Drawing.Size(80, 23);
                    portNumeric.Minimum = 1;
                    portNumeric.Maximum = 65535;
                    portNumeric.Value = 3260;

                    Button okButton = new Button();
                    okButton.Text = "OK";
                    okButton.DialogResult = DialogResult.OK;
                    okButton.Location = new System.Drawing.Point(120, 100);

                    Button cancelButton = new Button();
                    cancelButton.Text = "Cancel";
                    cancelButton.DialogResult = DialogResult.Cancel;
                    cancelButton.Location = new System.Drawing.Point(220, 100);

                    addPathDialog.Controls.Add(addressLabel);
                    addPathDialog.Controls.Add(addressTextBox);
                    addPathDialog.Controls.Add(portLabel);
                    addPathDialog.Controls.Add(portNumeric);
                    addPathDialog.Controls.Add(okButton);
                    addPathDialog.Controls.Add(cancelButton);

                    addPathDialog.AcceptButton = okButton;
                    addPathDialog.CancelButton = cancelButton;

                    if (addPathDialog.ShowDialog() == DialogResult.OK)
                    {
                        string address = addressTextBox.Text.Trim();
                        int port = (int)portNumeric.Value;

                        if (string.IsNullOrEmpty(address))
                        {
                            MessageBox.Show("Please enter a valid path address", "Input Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }

                        // Create new path
                        ISCSIPath newPath = new ISCSIPath
                        {
                            Address = address,
                            Port = port,
                            Status = PathStatus.Standby
                        };

                        // Add path to target
                        List<ISCSIPath> paths = new List<ISCSIPath> { newPath };
                        
                        if (!selectedTarget.IsMPIOEnabled)
                        {
                            // Enable MPIO with the new path
                            ComboBox policyComboBox = (ComboBox)Controls.Find("policyComboBox", true)[0];
                            MPIOPolicy policy = (MPIOPolicy)Enum.Parse(typeof(MPIOPolicy), policyComboBox.SelectedItem?.ToString() ?? "RoundRobin");
                            
                            bool success = _iscsiService.EnableMPIO(selectedTarget, paths, policy);
                            if (success)
                            {
                                MessageBox.Show("MPIO enabled and path added successfully", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }
                            else
                            {
                                MessageBox.Show("Failed to enable MPIO", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                        else
                        {
                            // Just add the new path
                            selectedTarget.Paths.Add(newPath);
                            MessageBox.Show("Path added successfully", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }

                        // Refresh the MPIO view
                        LoadConnectedTargets();
                        
                        // Select the target again to update path list
                        foreach (ListViewItem item in mpioListView.Items)
                        {
                            if (((ISCSITarget)item.Tag).TargetName == selectedTarget.TargetName)
                            {
                                item.Selected = true;
                                break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error adding path: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RemovePath_Click(object sender, EventArgs e)
        {
            try
            {
                ListView mpioListView = (ListView)Controls.Find("mpioListView", true)[0];
                ListView pathListView = (ListView)Controls.Find("pathListView", true)[0];
                
                if (mpioListView.SelectedItems.Count == 0 || pathListView.SelectedItems.Count == 0)
                {
                    MessageBox.Show("Please select a target and a path to remove", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                ISCSITarget selectedTarget = (ISCSITarget)mpioListView.SelectedItems[0].Tag;
                int selectedPathIndex = pathListView.SelectedIndices[0];
                
                if (selectedTarget.Paths.Count <= 1)
                {
                    MessageBox.Show("Cannot remove the last path. Use 'Disable MPIO' instead.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                
                // Remove the path
                selectedTarget.Paths.RemoveAt(selectedPathIndex);
                
                // Update the path list view
                pathListView.Items.RemoveAt(selectedPathIndex);
                
                // Update the MPIO list view
                mpioListView.SelectedItems[0].SubItems[4].Text = selectedTarget.Paths.Count.ToString();
                
                MessageBox.Show("Path removed successfully", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error removing path: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void EnableMPIO_Click(object sender, EventArgs e)
        {
            if (connectedTargetsListView.SelectedItems.Count == 0)
            {
                MessageBox.Show("Please select a connected target to configure MPIO.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            ISCSITarget selectedTarget = (ISCSITarget)connectedTargetsListView.SelectedItems[0].Tag;

            if (selectedTarget.IsMPIOEnabled)
            {
                MessageBox.Show("MPIO is already enabled for this target.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Collect paths from the ListView
            List<ISCSIPath> paths = new List<ISCSIPath>();
            foreach (ListViewItem item in mpioPathsListView.Items)
            {
                if (item.Tag is ISCSIPath path) // Ensure the tag is a valid path object
                {
                    paths.Add(path);
                }
                else if (item.SubItems.Count >= 2) // Fallback if Tag is not set correctly
                {
                    // Attempt to parse from ListView subitems (less reliable)
                    try
                    {
                        paths.Add(new ISCSIPath
                        {
                            Address = item.SubItems[0].Text,
                            Port = int.Parse(item.SubItems[1].Text),
                            Status = PathStatus.Unavailable // Initial status before enabling
                        });
                    }
                    catch (FormatException)
                    {
                        MessageBox.Show($"Invalid port number for path: {item.SubItems[0].Text}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }
            }

            if (paths.Count == 0)
            {
                 MessageBox.Show("Please add at least one path for MPIO.", "Input Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                 return;
            }

            MPIOPolicy policy = (MPIOPolicy)mpioPolicyComboBox.SelectedItem;

            statusLabel.Text = $"Enabling MPIO for {selectedTarget.TargetName}...";
            this.Cursor = Cursors.WaitCursor;

            try
            {
                bool success = await _iscsiService.EnableMPIOAsync(selectedTarget, paths, policy);
                if (success)
                {
                    statusLabel.Text = "MPIO enabled successfully.";
                    UpdateMPIOControls(selectedTarget);
                }
                else
                {
                    MessageBox.Show("Failed to enable MPIO.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    statusLabel.Text = "Failed to enable MPIO.";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error enabling MPIO: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                statusLabel.Text = "Error enabling MPIO.";
            }
            finally
            {
                this.Cursor = Cursors.Default;
            }
        }

        private async void DisableMPIO_Click(object sender, EventArgs e)
        {
            if (connectedTargetsListView.SelectedItems.Count == 0)
            {
                MessageBox.Show("Please select a connected target.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            ISCSITarget selectedTarget = (ISCSITarget)connectedTargetsListView.SelectedItems[0].Tag;

            if (!selectedTarget.IsMPIOEnabled)
            {
                MessageBox.Show("MPIO is not enabled for this target.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (MessageBox.Show("Are you sure you want to disable MPIO for this target?", "Confirm Disable MPIO", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                statusLabel.Text = $"Disabling MPIO for {selectedTarget.TargetName}...";
                this.Cursor = Cursors.WaitCursor;
                try
                {
                    bool success = await _iscsiService.DisableMPIOAsync(selectedTarget);
                    if (success)
                    {
                        statusLabel.Text = "MPIO disabled successfully.";
                        UpdateMPIOControls(selectedTarget);
                    }
                    else
                    {
                        MessageBox.Show("Failed to disable MPIO.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        statusLabel.Text = "Failed to disable MPIO.";
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error disabling MPIO: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    statusLabel.Text = "Error disabling MPIO.";
                }
                finally
                {
                    this.Cursor = Cursors.Default;
                }
            }
        }

        private async void PolicyChanged_Click(object sender, EventArgs e)
        {
            if (connectedTargetsListView.SelectedItems.Count == 0)
            {
                // No target selected, or policy changed programmatically, do nothing.
                return;
            }

            ISCSITarget selectedTarget = (ISCSITarget)connectedTargetsListView.SelectedItems[0].Tag;

            if (!selectedTarget.IsMPIOEnabled)
            {
                // MPIO not enabled, changing policy has no effect yet.
                return;
            }

            MPIOPolicy policy = (MPIOPolicy)mpioPolicyComboBox.SelectedItem;

            if (selectedTarget.LoadBalancePolicy == policy)
            {
                return; // No change
            }

            statusLabel.Text = $"Setting MPIO policy for {selectedTarget.TargetName}...";
            this.Cursor = Cursors.WaitCursor;
            try
            {
                bool success = await _iscsiService.SetLoadBalancePolicyAsync(selectedTarget, policy);
                if (success)
                {
                    statusLabel.Text = "MPIO policy updated successfully.";
                    // Update the target object in case the service modifies it
                    selectedTarget.LoadBalancePolicy = policy;
                }
                else
                {
                    MessageBox.Show("Failed to set MPIO policy.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    statusLabel.Text = "Failed to set MPIO policy.";
                    // Revert UI selection if the backend call failed
                    mpioPolicyComboBox.SelectedItem = selectedTarget.LoadBalancePolicy;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error setting MPIO policy: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                statusLabel.Text = "Error setting MPIO policy.";
                mpioPolicyComboBox.SelectedItem = selectedTarget.LoadBalancePolicy;
            }
            finally
            {
                this.Cursor = Cursors.Default;
            }
        }

        private async void RefreshMPIO_Click(object sender, EventArgs e)
        {
            if (connectedTargetsListView.SelectedItems.Count == 0)
            {
                MessageBox.Show("Please select a connected target to refresh MPIO status.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            ISCSITarget selectedTarget = (ISCSITarget)connectedTargetsListView.SelectedItems[0].Tag;
            UpdateMPIOControls(selectedTarget); // This might need to become async if GetPathStatusesAsync is slow
            // If GetPathStatusesAsync is implemented and potentially slow:
            /*
            statusLabel.Text = "Refreshing MPIO paths...";
            this.Cursor = Cursors.WaitCursor;
            try
            {
                await UpdateMPIOPathStatusesAsync(selectedTarget); // Assuming this method exists and is async
                statusLabel.Text = "MPIO paths refreshed.";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error refreshing MPIO paths: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                statusLabel.Text = "Error refreshing MPIO paths.";
            }
            finally
            {
                this.Cursor = Cursors.Default;
            }
            */
        }

        private void BrowseStorage_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Filter = "Virtual Hard Disk (*.vhdx)|*.vhdx|All files (*.*)|*.*";
                saveFileDialog.Title = "Select Backing Storage File Location";
                saveFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    storagePathTextBox.Text = saveFileDialog.FileName;
                }
            }
        }

        private async void CreateTarget_Click(object sender, EventArgs e)
        {
            string targetName = targetNameTextBox.Text.Trim();
            string storagePath = storagePathTextBox.Text.Trim();
            string sizeText = targetSizeTextBox.Text.Trim();
            string username = targetChapUserTextBox.Text.Trim();
            string password = targetChapPasswordTextBox.Text;

            if (string.IsNullOrEmpty(targetName) || !targetName.StartsWith("iqn."))
            {
                MessageBox.Show("Please enter a valid Target IQN (e.g., iqn.yyyy-mm.domain:identifier).", "Invalid Input", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrEmpty(storagePath))
            {
                MessageBox.Show("Please specify a path for the backing storage file (.vhdx).", "Invalid Input", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!int.TryParse(sizeText, out int sizeGB) || sizeGB <= 0)
            {
                MessageBox.Show("Please enter a valid positive number for the target size in GB.", "Invalid Input", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            bool useChap = targetChapCheckBox.Checked;
            if (useChap && (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password)))
            {
                MessageBox.Show("If CHAP is enabled, both username and password are required.", "Invalid Input", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            ISCSITarget newTarget = new ISCSITarget
            {
                TargetName = targetName,
                BackingStoragePath = storagePath,
                SizeGB = sizeGB,
                IsLocalTarget = true,
                PortalAddress = "127.0.0.1" // Default for local targets
            };

            statusLabel.Text = $"Creating target {targetName}...";
            this.Cursor = Cursors.WaitCursor;

            try
            {
                bool success = await _iscsiService.CreateTargetAsync(newTarget, useChap ? username : null, useChap ? password : null);
                if (success)
                {
                    statusLabel.Text = "Target created successfully.";
                    LoadLocalTargets(); // Refresh the list
                    // Clear input fields
                    targetNameTextBox.Clear();
                    storagePathTextBox.Clear();
                    targetSizeTextBox.Clear();
                    targetChapCheckBox.Checked = false;
                    targetChapUserTextBox.Clear();
                    targetChapPasswordTextBox.Clear();
                }
                else
                {
                    MessageBox.Show("Failed to create the target.", "Creation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    statusLabel.Text = "Target creation failed.";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating target: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                statusLabel.Text = "Target creation error.";
            }
            finally
            {
                this.Cursor = Cursors.Default;
            }
        }

        private async void DeleteTarget_Click(object sender, EventArgs e)
        {
            if (localTargetsListView.SelectedItems.Count == 0)
            {
                MessageBox.Show("Please select a local target to delete.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            ISCSITarget selectedTarget = (ISCSITarget)localTargetsListView.SelectedItems[0].Tag;

            if (MessageBox.Show($"Are you sure you want to delete the target '{selectedTarget.TargetName}'? This action cannot be undone.", "Confirm Deletion", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                statusLabel.Text = $"Deleting target {selectedTarget.TargetName}...";
                this.Cursor = Cursors.WaitCursor;
                try
                {
                    bool success = await _iscsiService.DeleteTargetAsync(selectedTarget);
                    if (success)
                    {
                        statusLabel.Text = "Target deleted successfully.";
                        LoadLocalTargets(); // Refresh the list
                    }
                    else
                    {
                        MessageBox.Show("Failed to delete the target.", "Deletion Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        statusLabel.Text = "Target deletion failed.";
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error deleting target: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    statusLabel.Text = "Target deletion error.";
                }
                finally
                {
                    this.Cursor = Cursors.Default;
                }
            }
        }

        private void RefreshTargets_Click(object sender, EventArgs e)
        {
            LoadLocalTargets();
        }

        // Helper method to refresh the connected targets list view
        private async void RefreshConnectedTargetsList()
        {
            connectedTargetsListView.Items.Clear();
            statusLabel.Text = "Refreshing connected targets...";
            this.Cursor = Cursors.WaitCursor;
            try
            {
                _connectedTargets = await _iscsiService.GetConnectedTargetsAsync();
                foreach (var target in _connectedTargets)
                {
                    var item = new ListViewItem(target.TargetName);
                    item.SubItems.Add(target.PortalAddress);
                    item.SubItems.Add(target.IsConnected ? "Connected" : "Disconnected");
                    item.SubItems.Add(target.SessionId ?? "N/A");
                    item.SubItems.Add(target.IsPersistent ? "Yes" : "No");
                    item.SubItems.Add(target.IsMPIOEnabled ? "Yes" : "No");
                    item.Tag = target;
                    connectedTargetsListView.Items.Add(item);
                }
                statusLabel.Text = $"Connected targets refreshed. Found {_connectedTargets.Count}.";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error refreshing connected targets: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                statusLabel.Text = "Error refreshing connected targets.";
            }
            finally
            {
                this.Cursor = Cursors.Default;
            }
        }
    }
}