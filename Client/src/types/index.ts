// Study types
export interface Study {
  id: number;
  studyInstanceUid: string;
  studyId?: string;
  studyDescription?: string;
  studyDate?: string;
  accessionNumber?: string;
  patientId?: string;
  patientName?: string;
  patientBirthDate?: string;
  patientSex?: string;
  patientAge?: string;
  institutionName?: string;
  numberOfSeries: number;
  numberOfInstances: number;
  createdAt: string;
  encryptedStudyUid?: string;
}

export interface StudyDetail extends Study {
  referringPhysicianName?: string;
  series: Series[];
}

// Series types
export interface Series {
  id: number;
  seriesInstanceUid: string;
  seriesNumber?: string;
  seriesDescription?: string;
  modality?: string;
  seriesDate?: string;
  bodyPartExamined?: string;
  rows?: number;
  columns?: number;
  numberOfInstances: number;
  thumbnailUrl?: string;
}

export interface SeriesDetail extends Series {
  protocolName?: string;
  sliceThickness?: number;
  instances: Instance[];
}

// Instance types
export interface Instance {
  id: number;
  sopInstanceUid: string;
  sopClassUid?: string;
  instanceNumber?: number;
  rows?: number;
  columns?: number;
  windowCenter?: number;
  windowWidth?: number;
  rescaleIntercept?: number;
  rescaleSlope?: number;
  numberOfFrames: number;
  thumbnailUrl?: string;
  imageUrl?: string;
}

export interface InstanceDetail extends Instance {
  bitsAllocated?: number;
  bitsStored?: number;
  photometricInterpretation?: string;
  pixelSpacing?: string;
  imagePositionPatient?: string;
  imageOrientationPatient?: string;
  sliceLocation?: number;
  frameTime?: number;
  transferSyntaxUid?: string;
  annotations: Annotation[];
  measurements: Measurement[];
}

// Annotation types
export interface Annotation {
  id: number;
  type: string;
  text?: string;
  color?: string;
  fontSize?: number;
  isVisible: boolean;
  positionData: string;
  frameNumber?: number;
  createdAt: string;
  createdBy?: string;
}

export interface CreateAnnotation {
  type: string;
  text?: string;
  color?: string;
  fontSize?: number;
  positionData: string;
  frameNumber?: number;
}

// Measurement types
export interface Measurement {
  id: number;
  type: string;
  value?: number;
  unit?: string;
  label?: string;
  color?: string;
  isVisible: boolean;
  mean?: number;
  stdDev?: number;
  min?: number;
  max?: number;
  area?: number;
  positionData: string;
  frameNumber?: number;
  createdAt: string;
  createdBy?: string;
}

export interface CreateMeasurement {
  type: string;
  value?: number;
  unit?: string;
  label?: string;
  color?: string;
  mean?: number;
  stdDev?: number;
  min?: number;
  max?: number;
  area?: number;
  positionData: string;
  frameNumber?: number;
}

// Window presets
export interface WindowPreset {
  name: string;
  windowCenter: number;
  windowWidth: number;
}

// DICOM Tags
export interface DicomTag {
  tag: string;
  name: string;
  vr: string;
  value: string;
  length?: number;
}

// Search/Query types
export interface StudySearch {
  patientId?: string;
  patientName?: string;
  studyDescription?: string;
  accessionNumber?: string;
  studyDateFrom?: string;
  studyDateTo?: string;
  modality?: string;
  page?: number;
  pageSize?: number;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

// Upload types
export interface UploadResult {
  success: boolean;
  message?: string;
  studiesProcessed: number;
  seriesProcessed: number;
  instancesProcessed: number;
  errors?: string[];
  studies?: Study[];
}

// Viewer state
export interface ViewerState {
  currentStudyId?: number;
  currentSeriesId?: number;
  currentInstanceId?: number;
  currentFrame: number;
  windowCenter: number;
  windowWidth: number;
  zoom: number;
  pan: { x: number; y: number };
  rotation: number;
  flipH: boolean;
  flipV: boolean;
  invert: boolean;
  activeTool: ToolType;
  layout: LayoutType;
  showAnnotations: boolean;
  showMeasurements: boolean;
  showOverlay: boolean;
}

export type ToolType = 
  | 'wwwc'
  | 'pan'
  | 'zoom'
  | 'length'
  | 'angle'
  | 'ellipseRoi'
  | 'rectangleRoi'
  | 'probe'
  | 'freehand'
  | 'arrow'
  | 'text';

export type LayoutType = '1x1' | '1x2' | '2x1' | '2x2' | '2x3' | '3x2' | '3x3';

// Hanging protocol
export interface HangingProtocol {
  id: number;
  name: string;
  description?: string;
  modality?: string;
  bodyPart?: string;
  layoutConfig: string;
  priority: number;
  isDefault: boolean;
}

// Export types
export interface ExportRequest {
  instanceId: number;
  frame?: number;
  format: 'png' | 'jpeg' | 'dicom';
  includeAnnotations: boolean;
  includeMeasurements: boolean;
  quality?: number;
}

// MPR types
export interface MprRequest {
  seriesInstanceUid: string;
  plane: 'axial' | 'sagittal' | 'coronal';
  sliceIndex: number;
  windowCenter?: number;
  windowWidth?: number;
}
