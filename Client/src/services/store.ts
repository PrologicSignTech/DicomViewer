import { create } from 'zustand';
import { 
  Study, 
  StudyDetail, 
  Series, 
  SeriesDetail, 
  Instance, 
  InstanceDetail,
  ViewerState,
  ToolType,
  LayoutType,
  WindowPreset,
  Measurement,
  Annotation,
} from '../types';
import apiService from '../services/api';

// Window presets
export const windowPresets: WindowPreset[] = [
  { name: 'CT Abdomen', windowCenter: 40, windowWidth: 400 },
  { name: 'CT Bone', windowCenter: 500, windowWidth: 2000 },
  { name: 'CT Brain', windowCenter: 40, windowWidth: 80 },
  { name: 'CT Chest', windowCenter: -600, windowWidth: 1500 },
  { name: 'CT Lung', windowCenter: -400, windowWidth: 1500 },
  { name: 'CT Liver', windowCenter: 60, windowWidth: 150 },
  { name: 'CT Soft Tissue', windowCenter: 40, windowWidth: 350 },
  { name: 'CT Stroke', windowCenter: 40, windowWidth: 40 },
  { name: 'MR T1', windowCenter: 500, windowWidth: 1000 },
  { name: 'MR T2', windowCenter: 300, windowWidth: 600 },
];

interface AppState {
  // Study List
  studies: Study[];
  studiesLoading: boolean;
  studiesError: string | null;
  totalStudies: number;
  currentPage: number;
  pageSize: number;

  // Current selections
  currentStudy: StudyDetail | null;
  currentSeries: SeriesDetail | null;
  currentInstance: InstanceDetail | null;
  
  // Viewer state
  viewer: ViewerState;
  
  // Measurements and Annotations
  measurements: Measurement[];
  annotations: Annotation[];

  // Upload state
  uploading: boolean;
  uploadProgress: number;

  // Actions
  fetchStudies: (page?: number, search?: Record<string, string>) => Promise<void>;
  selectStudy: (studyId: number) => Promise<void>;
  selectSeries: (seriesId: number) => Promise<void>;
  selectInstance: (instanceId: number) => Promise<void>;
  
  // Viewer actions
  setWindowLevel: (wc: number, ww: number) => void;
  applyPreset: (preset: WindowPreset) => void;
  setZoom: (zoom: number) => void;
  setPan: (x: number, y: number) => void;
  setRotation: (rotation: number) => void;
  toggleFlipH: () => void;
  toggleFlipV: () => void;
  toggleInvert: () => void;
  setActiveTool: (tool: ToolType) => void;
  setLayout: (layout: LayoutType) => void;
  setFrame: (frame: number) => void;
  resetViewer: () => void;
  toggleAnnotations: () => void;
  toggleMeasurements: () => void;
  toggleOverlay: () => void;

  // Upload actions
  uploadFiles: (files: File[]) => Promise<void>;

  // Measurement/Annotation actions
  addMeasurement: (measurement: Measurement) => void;
  removeMeasurement: (id: number) => void;
  addAnnotation: (annotation: Annotation) => void;
  removeAnnotation: (id: number) => void;
}

const initialViewerState: ViewerState = {
  currentFrame: 0,
  windowCenter: 40,
  windowWidth: 400,
  zoom: 1,
  pan: { x: 0, y: 0 },
  rotation: 0,
  flipH: false,
  flipV: false,
  invert: false,
  activeTool: 'wwwc',
  layout: '1x1',
  showAnnotations: true,
  showMeasurements: true,
  showOverlay: true,
};

export const useAppStore = create<AppState>((set, get) => ({
  // Initial state
  studies: [],
  studiesLoading: false,
  studiesError: null,
  totalStudies: 0,
  currentPage: 1,
  pageSize: 20,
  currentStudy: null,
  currentSeries: null,
  currentInstance: null,
  viewer: initialViewerState,
  measurements: [],
  annotations: [],
  uploading: false,
  uploadProgress: 0,

  // Fetch studies
  fetchStudies: async (page = 1, search = {}) => {
    set({ studiesLoading: true, studiesError: null });
    try {
      const result = await apiService.searchStudies({
        page,
        pageSize: get().pageSize,
        ...search,
      });
      set({
        studies: result.items,
        totalStudies: result.totalCount,
        currentPage: result.page,
        studiesLoading: false,
      });
    } catch (error: any) {
      set({
        studiesError: error.message || 'Failed to fetch studies',
        studiesLoading: false,
      });
    }
  },

  // Select study
  selectStudy: async (studyId: number) => {
    try {
      const study = await apiService.getStudyById(studyId);
      set({ currentStudy: study, currentSeries: null, currentInstance: null });
      
      // Auto-select first series
      if (study.series.length > 0) {
        await get().selectSeries(study.series[0].id);
      }
    } catch (error: any) {
      console.error('Failed to select study:', error);
    }
  },

  // Select series
  selectSeries: async (seriesId: number) => {
    try {
      const series = await apiService.getSeriesById(seriesId);
      set({ currentSeries: series, currentInstance: null });
      
      // Auto-select first instance
      if (series.instances.length > 0) {
        await get().selectInstance(series.instances[0].id);
      }
    } catch (error: any) {
      console.error('Failed to select series:', error);
    }
  },

  // Select instance
  selectInstance: async (instanceId: number) => {
    try {
      const instance = await apiService.getInstanceById(instanceId);
      set({ 
        currentInstance: instance,
        measurements: instance.measurements,
        annotations: instance.annotations,
        viewer: {
          ...get().viewer,
          currentFrame: 0,
          windowCenter: instance.windowCenter ?? 40,
          windowWidth: instance.windowWidth ?? 400,
        },
      });
    } catch (error: any) {
      console.error('Failed to select instance:', error);
    }
  },

  // Viewer actions
  setWindowLevel: (wc: number, ww: number) => {
    set((state) => ({
      viewer: { ...state.viewer, windowCenter: wc, windowWidth: ww },
    }));
  },

  applyPreset: (preset: WindowPreset) => {
    set((state) => ({
      viewer: {
        ...state.viewer,
        windowCenter: preset.windowCenter,
        windowWidth: preset.windowWidth,
      },
    }));
  },

  setZoom: (zoom: number) => {
    set((state) => ({
      viewer: { ...state.viewer, zoom: Math.max(0.1, Math.min(10, zoom)) },
    }));
  },

  setPan: (x: number, y: number) => {
    set((state) => ({
      viewer: { ...state.viewer, pan: { x, y } },
    }));
  },

  setRotation: (rotation: number) => {
    set((state) => ({
      viewer: { ...state.viewer, rotation: rotation % 360 },
    }));
  },

  toggleFlipH: () => {
    set((state) => ({
      viewer: { ...state.viewer, flipH: !state.viewer.flipH },
    }));
  },

  toggleFlipV: () => {
    set((state) => ({
      viewer: { ...state.viewer, flipV: !state.viewer.flipV },
    }));
  },

  toggleInvert: () => {
    set((state) => ({
      viewer: { ...state.viewer, invert: !state.viewer.invert },
    }));
  },

  setActiveTool: (tool: ToolType) => {
    set((state) => ({
      viewer: { ...state.viewer, activeTool: tool },
    }));
  },

  setLayout: (layout: LayoutType) => {
    set((state) => ({
      viewer: { ...state.viewer, layout },
    }));
  },

  setFrame: (frame: number) => {
    const instance = get().currentInstance;
    if (instance) {
      const maxFrame = instance.numberOfFrames - 1;
      set((state) => ({
        viewer: {
          ...state.viewer,
          currentFrame: Math.max(0, Math.min(maxFrame, frame)),
        },
      }));
    }
  },

  resetViewer: () => {
    const instance = get().currentInstance;
    set((state) => ({
      viewer: {
        ...initialViewerState,
        windowCenter: instance?.windowCenter ?? 40,
        windowWidth: instance?.windowWidth ?? 400,
      },
    }));
  },

  toggleAnnotations: () => {
    set((state) => ({
      viewer: { ...state.viewer, showAnnotations: !state.viewer.showAnnotations },
    }));
  },

  toggleMeasurements: () => {
    set((state) => ({
      viewer: { ...state.viewer, showMeasurements: !state.viewer.showMeasurements },
    }));
  },

  toggleOverlay: () => {
    set((state) => ({
      viewer: { ...state.viewer, showOverlay: !state.viewer.showOverlay },
    }));
  },

  // Upload
  uploadFiles: async (files: File[]) => {
    set({ uploading: true, uploadProgress: 0 });
    try {
      const result = await apiService.uploadFiles(files);
      set({ uploading: false, uploadProgress: 100 });
      
      // Refresh study list
      await get().fetchStudies();
      
      // Select first uploaded study
      if (result.studies && result.studies.length > 0) {
        await get().selectStudy(result.studies[0].id);
      }
    } catch (error: any) {
      set({ uploading: false });
      throw error;
    }
  },

  // Measurement/Annotation actions
  addMeasurement: (measurement: Measurement) => {
    set((state) => ({
      measurements: [...state.measurements, measurement],
    }));
  },

  removeMeasurement: (id: number) => {
    set((state) => ({
      measurements: state.measurements.filter((m) => m.id !== id),
    }));
  },

  addAnnotation: (annotation: Annotation) => {
    set((state) => ({
      annotations: [...state.annotations, annotation],
    }));
  },

  removeAnnotation: (id: number) => {
    set((state) => ({
      annotations: state.annotations.filter((a) => a.id !== id),
    }));
  },
}));

export default useAppStore;
