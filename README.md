# DicomViewer
This DICOM viewer application has been configured with all features visible by default for immediate access.
# DICOM Viewer - Available Features Guide

## Overview
This DICOM viewer application has been configured with all features visible by default for immediate access.

## Main Features Always Visible

### 1. Study List (Home Page - `/`)
- **View All Studies**: Searchable table with all uploaded DICOM studies
- **Upload DICOM Files**: Drag & drop or browse to upload
- **Share Study**: Click Share button to get encrypted link for any study
- **Study Information**: Patient name, ID, study description, date, modality, series count

### 2. Advanced Viewer (`/viewer/:studyId` or `/view/:encryptedStudyUid`)

#### Left Panel - Series Browser
- **Series Thumbnails**: Visual preview of all series in the study
- **Instance Navigation**: Browse through all images in each series
- **Series Information**: Modality, series number, instance count

#### Center - Main Viewport
All viewing modes accessible via toolbar:
- **Single View**: Standard single image display
- **MPR View**: Multi-planar reconstruction (Axial, Sagittal, Coronal)
- **Compare**: Side-by-side comparison with prior studies
- **3D Volume**: 3D volume rendering

#### Toolbar - All Tools Visible
**Basic Tools:**
- Zoom (Z)
- Pan (P)
- Window/Level (W)
- Rotate

**Measurement Tools:**
- Length measurement
- Angle measurement
- Ellipse ROI
- Rectangle ROI
- Freehand ROI

**Image Manipulation:**
- Rotate 90°
- Flip Horizontal (H)
- Flip Vertical (V)
- Invert (I)
- Reset (R)

**Advanced Features:**
- Window presets (CT Abdomen, Brain, Lung, Bone, etc.)
- LUT/Color mapping
- Image enhancement (sharpen, smooth, contrast, brightness)
- Key image marking
- Cine playback for multi-frame images

#### Right Panels - Always Visible

**DICOM Tags Panel:**
- View all DICOM metadata
- Search and filter tags
- Pixel value inspection

**Measurements Panel:**
- List of all measurements
- Measurement statistics (length, area, angle values)
- Annotation management
- Export measurements

**Report Panel:**
- Create structured reports
- Add findings and impressions
- Export reports (HTML, PDF, DICOM-SR)

## How to Use

### Viewing a Study
1. Go to Study List (home page)
2. Click on any study row or click the "View" icon
3. All panels and tools are immediately available

### Sharing a Study
1. In Study List, click the "Share" icon for any study
2. Copy the encrypted link from the dialog
3. Share the link - recipients can open it directly without login

### Making Measurements
1. Open a study in the viewer
2. Select a measurement tool from the toolbar (Length, Angle, ROI)
3. Draw on the image
4. View measurements in the right panel
5. Measurements are automatically saved

### Using Window/Level
1. Click the Window/Level tool or press 'W'
2. Drag mouse: Left/Right = Window Width, Up/Down = Window Center
3. Or use preset buttons for common anatomies

### Viewing Multiple Series
1. Series thumbnails appear in the left panel
2. Click any series to load it in the main viewport
3. Use MPR or Compare mode for multi-series viewing

## Encrypted Study Links

### Format
`https://your-domain.com/view/AbC123XyZ...`

### Benefits
- Direct access to specific studies
- No need to navigate through study list
- Secure AES-256 encryption
- URL-safe for sharing via email, messaging, etc.

## Additional Components Available (Not Integrated in Main Viewer)

These advanced components are available in the codebase but not integrated into the main viewer:

1. **HangingProtocolSelector** - Preset viewport layouts
2. **SegmentationTools** - Manual and semi-automatic segmentation
3. **TimeSeriesControls** - 4D/time-series playback
4. **FusionOverlayControls** - Multi-modality fusion (CT-PET, etc.)

## Technical Details

### Supported Modalities
- CT (Computed Tomography)
- MR (Magnetic Resonance)
- CR/DX (X-ray)
- US (Ultrasound)
- PT (PET)
- NM (Nuclear Medicine)
- MG (Mammography)

### Supported File Formats
- DICOM (.dcm, .dicom)
- DICOM files without extension

### Backend APIs Available
- Study management (`/api/studies`)
- Series operations (`/api/series`)
- Instance operations (`/api/instances`)
- Measurements (`/api/measurements`)
- Workflow/Hanging protocols (`/api/workflow`)
- Reports (`/api/reports`)
- Advanced imaging (`/api/advancedimaging`) - MPR, 3D, MIP, fusion

## Customization

All panels can be toggled on/off using the toolbar buttons:
- DICOM Tags: Info icon
- Measurements: Assessment icon
- Reports: Description icon

Default state is now "all visible" for immediate access to all features.
