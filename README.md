# Windows iSCSI Initiator Application

A Windows desktop application that allows users to discover, connect to, and manage iSCSI targets like StarWind SAN servers.

## Features

- Discover iSCSI targets on the network
- Connect to and disconnect from iSCSI targets
- Manage persistent connections
- View connected targets and their properties
- User-friendly interface with proper error handling

## Technical Details

This application uses the Windows iSCSI API to interact with the iSCSI service on Windows. It's built using:

- C# and .NET Framework
- Windows Forms for the user interface
- Windows iSCSI API for iSCSI operations

## Project Structure

- `src/` - Source code
  - `ISCSIApp/` - Main application code
    - `Models/` - Data models
    - `Services/` - iSCSI API interaction
    - `UI/` - User interface components
- `docs/` - Documentation