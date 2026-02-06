import React, { useEffect, useRef, useState, useCallback } from 'react';
import { useParams } from 'react-router-dom';
import {
  Box,
  IconButton,
  Tooltip,
  Typography,
  Slider,
  Divider,
  ToggleButton,
  ToggleButtonGroup,
  Menu,
  MenuItem,
  ListItemText,
  Paper,
  Tabs,
  Tab,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Button,
  TextField,
  Select,
  FormControl,
  InputLabel,
  Drawer,
  List,
  ListItem,
  ListItemIcon,
  Chip,
  LinearProgress,
  Badge,
} from '@mui/material';
import {
  ZoomIn,
  PanTool,
  Straighten,
  Brightness6,
  Flip,
  RotateRight,
  Refresh,
  GridOn,
  Info,
  TextFields,
  Circle,
  RadioButtonUnchecked,
  ArrowForward,
  PlayArrow,
  Pause,
  SkipNext,
  SkipPrevious,
  FirstPage,
  LastPage,
  Contrast,
  FlipCameraAndroid,
  ChangeHistory,
  ViewInAr,
  Layers,
  Compare,
  Bookmark,
  Star,
  StarBorder,
  Description,
  Timeline,
  Assessment,
  Download,
  Settings,
  Tune,
  BlurOn,
  PhotoFilter,
  Palette,
  ThreeDRotation,
} from '@mui/icons-material';
import useAppStore, { windowPresets } from '../services/store';
import apiService from '../services/api';
import SeriesPanel from './SeriesPanel';
import DicomTagsViewer from './DicomTagsViewer';
import MprViewer from './MprViewer';
import MeasurementPanel from './MeasurementPanel';
import ReportPanel from './ReportPanel';
import { ToolType } from '../types';

const AdvancedViewer: React.FC = () => {
  const { studyId, encryptedStudyUid } = useParams();
  const imageRef = useRef<HTMLImageElement>(null);
  const containerRef = useRef<HTMLDivElement>(null);

  const {
    currentStudy,
    currentSeries,
    currentInstance,
    viewer,
    selectStudy,
    selectInstance,
    setWindowLevel,
    applyPreset,
    setZoom,
    setPan,
    setRotation,
    toggleFlipH,
    toggleFlipV,
    toggleInvert,
    setActiveTool,
    setFrame,
    resetViewer,
  } = useAppStore();

  // State
  const [imageUrl, setImageUrl] = useState<string>('');
  const [isPlaying, setIsPlaying] = useState(false);
  const [presetAnchor, setPresetAnchor] = useState<null | HTMLElement>(null);
  const [layoutAnchor, setLayoutAnchor] = useState<null | HTMLElement>(null);
  const [showTags, setShowTags] = useState(true);
  const [mouseDown, setMouseDown] = useState(false);
  const [lastMousePos, setLastMousePos] = useState({ x: 0, y: 0 });
  const [pixelValue, setPixelValue] = useState<{ x: number; y: number; value: number; unit: string } | null>(null);
  
  // Advanced features state
  const [viewMode, setViewMode] = useState<'single' | 'mpr' | 'compare' | '3d'>('single');
  const [rightPanelTab, setRightPanelTab] = useState(0);
  const [showMeasurementPanel, setShowMeasurementPanel] = useState(true);
  const [showReportPanel, setShowReportPanel] = useState(true);
  const [showEnhancementDialog, setShowEnhancementDialog] = useState(false);
  const [enhancementSettings, setEnhancementSettings] = useState({
    sharpen: 0,
    smooth: 0,
    noiseReduction: 0,
    edgeEnhancement: 0,
    brightness: 0,
    contrast: 0,
    gamma: 1.0,
  });
  const [activeLut, setActiveLut] = useState<string>('');
  const [keyImages, setKeyImages] = useState<any[]>([]);
  const [priorStudies, setPriorStudies] = useState<any[]>([]);
  const [comparisonStudyId, setComparisonStudyId] = useState<number | null>(null);
  
  // Layout state
  const [layout, setLayout] = useState({ rows: 1, cols: 1 });
  const [activeViewport, setActiveViewport] = useState(0);

  // Load study
  useEffect(() => {
    const loadStudy = async () => {
      try {
        if (encryptedStudyUid) {
          // Load study by encrypted UID
          const studyDetail = await apiService.getStudyByEncryptedUid(encryptedStudyUid);
          if (studyDetail) {
            await selectStudy(studyDetail.id);
            loadPriorStudies(studyDetail.id);
            loadKeyImages(studyDetail.id);
          }
        } else if (studyId) {
          // Load study by ID
          await selectStudy(parseInt(studyId));
          loadPriorStudies(parseInt(studyId));
          loadKeyImages(parseInt(studyId));
        }
      } catch (error) {
        console.error('Error loading study:', error);
      }
    };
    loadStudy();
  }, [studyId, encryptedStudyUid]);

  // Update image URL
  useEffect(() => {
    if (currentInstance) {
      let url = apiService.getImageUrl(
        currentInstance.id,
        viewer.currentFrame,
        viewer.windowCenter,
        viewer.windowWidth,
        viewer.invert
      );
      
      if (activeLut) {
        url = `${apiService.getImageUrl(currentInstance.id, viewer.currentFrame)}&lut=${activeLut}`;
      }
      
      setImageUrl(url);
    }
  }, [currentInstance, viewer.currentFrame, viewer.windowCenter, viewer.windowWidth, viewer.invert, activeLut]);

  // Cine playback
  useEffect(() => {
    let interval: NodeJS.Timeout;
    if (isPlaying && currentInstance && currentInstance.numberOfFrames > 1) {
      const frameTime = currentInstance.frameTime || 100;
      interval = setInterval(() => {
        setFrame((viewer.currentFrame + 1) % currentInstance.numberOfFrames);
      }, frameTime);
    }
    return () => clearInterval(interval);
  }, [isPlaying, currentInstance, viewer.currentFrame]);

  // Keyboard shortcuts
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.target instanceof HTMLInputElement || e.target instanceof HTMLTextAreaElement) return;
      
      switch (e.key) {
        case 'ArrowRight':
        case 'ArrowDown':
          if (currentInstance && currentInstance.numberOfFrames > 1) {
            setFrame(Math.min(viewer.currentFrame + 1, currentInstance.numberOfFrames - 1));
          }
          break;
        case 'ArrowLeft':
        case 'ArrowUp':
          setFrame(Math.max(viewer.currentFrame - 1, 0));
          break;
        case ' ':
          e.preventDefault();
          if (currentInstance && currentInstance.numberOfFrames > 1) {
            setIsPlaying(!isPlaying);
          }
          break;
        case 'r':
          resetViewer();
          break;
        case 'i':
          toggleInvert();
          break;
        case 'h':
          toggleFlipH();
          break;
        case 'v':
          toggleFlipV();
          break;
        case 'm':
          setViewMode(viewMode === 'mpr' ? 'single' : 'mpr');
          break;
        case 'k':
          if (currentInstance) markAsKeyImage();
          break;
        case '1':
          setLayout({ rows: 1, cols: 1 });
          break;
        case '2':
          setLayout({ rows: 1, cols: 2 });
          break;
        case '4':
          setLayout({ rows: 2, cols: 2 });
          break;
      }
    };

    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [viewer.currentFrame, isPlaying, currentInstance, viewMode]);

  const loadPriorStudies = async (currentStudyId: number) => {
    try {
      const response = await fetch(`/api/workflow/studies/${currentStudyId}/priors`);
      const data = await response.json();
      setPriorStudies(data);
    } catch (error) {
      console.error('Error loading prior studies:', error);
    }
  };

  const loadKeyImages = async (currentStudyId: number) => {
    try {
      const response = await fetch(`/api/workflow/studies/${currentStudyId}/key-images`);
      const data = await response.json();
      setKeyImages(data);
    } catch (error) {
      console.error('Error loading key images:', error);
    }
  };

  const markAsKeyImage = async () => {
    if (!currentInstance || !currentStudy) return;
    
    try {
      await fetch(`/api/workflow/instances/${currentInstance.id}/key-image`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          frame: viewer.currentFrame,
          description: '',
          category: 'Finding',
          windowCenter: viewer.windowCenter,
          windowWidth: viewer.windowWidth,
        }),
      });
      loadKeyImages(currentStudy.id);
    } catch (error) {
      console.error('Error marking key image:', error);
    }
  };

  // Mouse handlers
  const handleMouseDown = (e: React.MouseEvent) => {
    setMouseDown(true);
    setLastMousePos({ x: e.clientX, y: e.clientY });
  };

  const handleMouseMove = (e: React.MouseEvent) => {
    if (!mouseDown) return;

    const deltaX = e.clientX - lastMousePos.x;
    const deltaY = e.clientY - lastMousePos.y;

    if (viewer.activeTool === 'wwwc') {
      const newWW = Math.max(1, viewer.windowWidth + deltaX * 2);
      const newWC = viewer.windowCenter - deltaY * 2;
      setWindowLevel(newWC, newWW);
    } else if (viewer.activeTool === 'pan') {
      setPan(viewer.pan.x + deltaX, viewer.pan.y + deltaY);
    } else if (viewer.activeTool === 'zoom') {
      const zoomDelta = -deltaY * 0.01;
      setZoom(viewer.zoom + zoomDelta);
    }

    setLastMousePos({ x: e.clientX, y: e.clientY });
  };

  const handleMouseUp = () => setMouseDown(false);

  const handleWheel = (e: React.WheelEvent) => {
    e.preventDefault();
    if (e.ctrlKey) {
      const zoomDelta = -e.deltaY * 0.001;
      setZoom(viewer.zoom + zoomDelta);
    } else {
      if (currentInstance && currentInstance.numberOfFrames > 1) {
        const delta = e.deltaY > 0 ? 1 : -1;
        setFrame(Math.max(0, Math.min(currentInstance.numberOfFrames - 1, viewer.currentFrame + delta)));
      } else if (currentSeries && currentSeries.instances.length > 1) {
        const currentIndex = currentSeries.instances.findIndex(i => i.id === currentInstance?.id);
        const delta = e.deltaY > 0 ? 1 : -1;
        const newIndex = Math.max(0, Math.min(currentSeries.instances.length - 1, currentIndex + delta));
        if (currentSeries.instances[newIndex]) {
          selectInstance(currentSeries.instances[newIndex].id);
        }
      }
    }
  };

  const handleImageClick = async (e: React.MouseEvent<HTMLImageElement>) => {
    if (!currentInstance || !imageRef.current) return;
    
    const rect = imageRef.current.getBoundingClientRect();
    const x = Math.floor((e.clientX - rect.left) / (rect.width / (currentInstance.columns || 512)));
    const y = Math.floor((e.clientY - rect.top) / (rect.height / (currentInstance.rows || 512)));

    if (viewer.activeTool === 'probe') {
      try {
        const result = await apiService.getPixelValue(currentInstance.id, x, y, viewer.currentFrame);
        setPixelValue(result);
      } catch (error) {
        console.error('Error getting pixel value:', error);
      }
    }
  };

  const tools: { id: ToolType; icon: React.ReactNode; label: string }[] = [
    { id: 'wwwc', icon: <Contrast />, label: 'Window/Level (W)' },
    { id: 'pan', icon: <PanTool />, label: 'Pan (P)' },
    { id: 'zoom', icon: <ZoomIn />, label: 'Zoom (Z)' },
    { id: 'length', icon: <Straighten />, label: 'Length' },
    { id: 'angle', icon: <ChangeHistory />, label: 'Angle' },
    { id: 'ellipseRoi', icon: <RadioButtonUnchecked />, label: 'Ellipse ROI' },
    { id: 'rectangleRoi', icon: <Circle />, label: 'Rectangle ROI' },
    { id: 'probe', icon: <Info />, label: 'Probe (HU)' },
    { id: 'text', icon: <TextFields />, label: 'Text Annotation' },
    { id: 'arrow', icon: <ArrowForward />, label: 'Arrow Annotation' },
  ];

  const imageTransform = `
    translate(${viewer.pan.x}px, ${viewer.pan.y}px)
    scale(${viewer.zoom})
    rotate(${viewer.rotation}deg)
    scaleX(${viewer.flipH ? -1 : 1})
    scaleY(${viewer.flipV ? -1 : 1})
  `;

  // Available LUTs
  const luts = [
    { name: '', label: 'Grayscale' },
    { name: 'hot', label: 'Hot' },
    { name: 'cool', label: 'Cool' },
    { name: 'rainbow', label: 'Rainbow' },
    { name: 'bone', label: 'Bone' },
    { name: 'cardiac', label: 'Cardiac' },
    { name: 'pet', label: 'PET' },
  ];

  return (
    <Box sx={{ display: 'flex', height: 'calc(100vh - 48px)', overflow: 'hidden' }}>
      {/* Left Panel - Series Thumbnails */}
      <SeriesPanel />

      {/* Main Viewer Area */}
      <Box sx={{ flex: 1, display: 'flex', flexDirection: 'column' }}>
        {/* Main Toolbar */}
        <Paper
          sx={{
            display: 'flex',
            alignItems: 'center',
            gap: 0.5,
            p: 0.5,
            borderRadius: 0,
            backgroundColor: '#1a1a1a',
            borderBottom: '1px solid #333',
            flexWrap: 'wrap',
          }}
        >
          {/* View Mode */}
          <ToggleButtonGroup
            value={viewMode}
            exclusive
            onChange={(_, v) => v && setViewMode(v)}
            size="small"
          >
            <ToggleButton value="single">
              <Tooltip title="Single View">
                <GridOn />
              </Tooltip>
            </ToggleButton>
            <ToggleButton value="mpr">
              <Tooltip title="MPR View (M)">
                <Layers />
              </Tooltip>
            </ToggleButton>
            <ToggleButton value="compare">
              <Tooltip title="Compare with Prior">
                <Compare />
              </Tooltip>
            </ToggleButton>
            <ToggleButton value="3d">
              <Tooltip title="3D Volume">
                <ThreeDRotation />
              </Tooltip>
            </ToggleButton>
          </ToggleButtonGroup>

          <Divider orientation="vertical" flexItem sx={{ mx: 0.5 }} />

          {/* Tool Selection */}
          <ToggleButtonGroup
            value={viewer.activeTool}
            exclusive
            onChange={(_, value) => value && setActiveTool(value)}
            size="small"
          >
            {tools.slice(0, 4).map((tool) => (
              <ToggleButton key={tool.id} value={tool.id}>
                <Tooltip title={tool.label}>{tool.icon}</Tooltip>
              </ToggleButton>
            ))}
          </ToggleButtonGroup>

          <Divider orientation="vertical" flexItem sx={{ mx: 0.5 }} />

          {/* Measurement Tools */}
          <ToggleButtonGroup
            value={viewer.activeTool}
            exclusive
            onChange={(_, value) => value && setActiveTool(value)}
            size="small"
          >
            {tools.slice(4, 8).map((tool) => (
              <ToggleButton key={tool.id} value={tool.id}>
                <Tooltip title={tool.label}>{tool.icon}</Tooltip>
              </ToggleButton>
            ))}
          </ToggleButtonGroup>

          <Divider orientation="vertical" flexItem sx={{ mx: 0.5 }} />

          {/* Window Presets */}
          <Tooltip title="Window Presets">
            <IconButton size="small" onClick={(e) => setPresetAnchor(e.currentTarget)}>
              <Brightness6 />
            </IconButton>
          </Tooltip>
          <Menu anchorEl={presetAnchor} open={Boolean(presetAnchor)} onClose={() => setPresetAnchor(null)}>
            {windowPresets.map((preset) => (
              <MenuItem key={preset.name} onClick={() => { applyPreset(preset); setPresetAnchor(null); }}>
                <ListItemText primary={preset.name} secondary={`WC: ${preset.windowCenter} WW: ${preset.windowWidth}`} />
              </MenuItem>
            ))}
          </Menu>

          {/* LUT Selection */}
          <FormControl size="small" sx={{ minWidth: 100 }}>
            <Select
              value={activeLut}
              onChange={(e) => setActiveLut(e.target.value)}
              displayEmpty
              sx={{ height: 32 }}
            >
              {luts.map((lut) => (
                <MenuItem key={lut.name} value={lut.name}>{lut.label}</MenuItem>
              ))}
            </Select>
          </FormControl>

          <Divider orientation="vertical" flexItem sx={{ mx: 0.5 }} />

          {/* Image Manipulation */}
          <Tooltip title="Rotate 90°">
            <IconButton size="small" onClick={() => setRotation(viewer.rotation + 90)}>
              <RotateRight />
            </IconButton>
          </Tooltip>
          <Tooltip title="Flip Horizontal (H)">
            <IconButton size="small" onClick={toggleFlipH} color={viewer.flipH ? 'primary' : 'default'}>
              <Flip />
            </IconButton>
          </Tooltip>
          <Tooltip title="Flip Vertical (V)">
            <IconButton size="small" onClick={toggleFlipV} color={viewer.flipV ? 'primary' : 'default'}>
              <Flip sx={{ transform: 'rotate(90deg)' }} />
            </IconButton>
          </Tooltip>
          <Tooltip title="Invert (I)">
            <IconButton size="small" onClick={toggleInvert} color={viewer.invert ? 'primary' : 'default'}>
              <FlipCameraAndroid />
            </IconButton>
          </Tooltip>

          {/* Image Enhancement */}
          <Tooltip title="Image Enhancement">
            <IconButton size="small" onClick={() => setShowEnhancementDialog(true)}>
              <Tune />
            </IconButton>
          </Tooltip>

          <Divider orientation="vertical" flexItem sx={{ mx: 0.5 }} />

          {/* Reset */}
          <Tooltip title="Reset (R)">
            <IconButton size="small" onClick={resetViewer}>
              <Refresh />
            </IconButton>
          </Tooltip>

          {/* Key Image */}
          <Tooltip title="Mark as Key Image (K)">
            <IconButton size="small" onClick={markAsKeyImage}>
              <Badge badgeContent={keyImages.length} color="primary">
                <Star />
              </Badge>
            </IconButton>
          </Tooltip>

          {/* DICOM Tags */}
          <Tooltip title="DICOM Tags">
            <IconButton size="small" onClick={() => setShowTags(!showTags)} color={showTags ? 'primary' : 'default'}>
              <Info />
            </IconButton>
          </Tooltip>

          {/* Measurements Panel */}
          <Tooltip title="Measurements">
            <IconButton size="small" onClick={() => setShowMeasurementPanel(!showMeasurementPanel)}>
              <Assessment />
            </IconButton>
          </Tooltip>

          {/* Report */}
          <Tooltip title="Create Report">
            <IconButton size="small" onClick={() => setShowReportPanel(!showReportPanel)}>
              <Description />
            </IconButton>
          </Tooltip>

          <Box sx={{ flexGrow: 1 }} />

          {/* Window/Level Display */}
          <Typography variant="caption" sx={{ mr: 2 }}>
            WC: {Math.round(viewer.windowCenter)} | WW: {Math.round(viewer.windowWidth)} | Zoom: {(viewer.zoom * 100).toFixed(0)}%
          </Typography>
        </Paper>

        {/* Image Viewport(s) */}
        {viewMode === 'single' && (
          <Box
            ref={containerRef}
            sx={{
              flex: 1,
              backgroundColor: '#000',
              position: 'relative',
              overflow: 'hidden',
              cursor: viewer.activeTool === 'pan' ? 'grab' : 
                     viewer.activeTool === 'zoom' ? 'zoom-in' : 
                     viewer.activeTool === 'wwwc' ? 'crosshair' : 'default',
            }}
            onMouseDown={handleMouseDown}
            onMouseMove={handleMouseMove}
            onMouseUp={handleMouseUp}
            onMouseLeave={handleMouseUp}
            onWheel={handleWheel}
          >
            {currentInstance && imageUrl ? (
              <>
                <img
                  ref={imageRef}
                  src={imageUrl}
                  alt="DICOM"
                  onClick={handleImageClick}
                  style={{
                    position: 'absolute',
                    top: '50%',
                    left: '50%',
                    maxWidth: '100%',
                    maxHeight: '100%',
                    transform: `translate(-50%, -50%) ${imageTransform}`,
                    transformOrigin: 'center center',
                    userSelect: 'none',
                    pointerEvents: 'auto',
                  }}
                  draggable={false}
                />

                {/* Overlay */}
                {viewer.showOverlay && (
                  <>
                    <Box sx={{ position: 'absolute', top: 8, left: 8, color: '#fff', textShadow: '1px 1px 2px #000', fontSize: '12px', fontFamily: 'monospace' }}>
                      <div>{currentStudy?.patientName || 'Unknown'}</div>
                      <div>ID: {currentStudy?.patientId || '-'}</div>
                      <div>{currentStudy?.studyDescription || ''}</div>
                    </Box>
                    <Box sx={{ position: 'absolute', top: 8, right: 8, color: '#fff', textShadow: '1px 1px 2px #000', fontSize: '12px', fontFamily: 'monospace', textAlign: 'right' }}>
                      <div>{currentSeries?.modality} - {currentSeries?.seriesDescription || ''}</div>
                      <div>{currentInstance.columns} x {currentInstance.rows}</div>
                    </Box>
                    <Box sx={{ position: 'absolute', bottom: 8, left: 8, color: '#fff', textShadow: '1px 1px 2px #000', fontSize: '12px', fontFamily: 'monospace' }}>
                      <div>WC: {Math.round(viewer.windowCenter)} WW: {Math.round(viewer.windowWidth)}</div>
                      <div>Zoom: {(viewer.zoom * 100).toFixed(0)}%</div>
                      {pixelValue && <div>Pixel ({pixelValue.x}, {pixelValue.y}): {pixelValue.value.toFixed(1)} {pixelValue.unit}</div>}
                    </Box>
                    <Box sx={{ position: 'absolute', bottom: 8, right: 8, color: '#fff', textShadow: '1px 1px 2px #000', fontSize: '12px', fontFamily: 'monospace', textAlign: 'right' }}>
                      {currentInstance.numberOfFrames > 1 && <div>Frame: {viewer.currentFrame + 1} / {currentInstance.numberOfFrames}</div>}
                      {currentSeries && <div>Image: {currentSeries.instances.findIndex(i => i.id === currentInstance.id) + 1} / {currentSeries.instances.length}</div>}
                    </Box>
                  </>
                )}
              </>
            ) : (
              <Box sx={{ position: 'absolute', top: '50%', left: '50%', transform: 'translate(-50%, -50%)', textAlign: 'center', color: 'text.secondary' }}>
                <Typography variant="h6">No image selected</Typography>
                <Typography variant="body2">Select a study from the list to view</Typography>
              </Box>
            )}
          </Box>
        )}

        {viewMode === 'mpr' && currentSeries && (
          <MprViewer seriesId={currentSeries.id} windowCenter={viewer.windowCenter} windowWidth={viewer.windowWidth} />
        )}

        {viewMode === 'compare' && (
          <Box sx={{ flex: 1, display: 'flex' }}>
            {/* Current study viewport */}
            <Box sx={{ flex: 1, backgroundColor: '#000', position: 'relative', borderRight: '2px solid #4fc3f7' }}>
              {currentInstance && imageUrl && (
                <img src={imageUrl} alt="Current" style={{ width: '100%', height: '100%', objectFit: 'contain' }} />
              )}
              <Chip label="Current" size="small" sx={{ position: 'absolute', top: 8, left: 8 }} color="primary" />
            </Box>
            {/* Prior study viewport */}
            <Box sx={{ flex: 1, backgroundColor: '#000', position: 'relative' }}>
              {priorStudies.length > 0 ? (
                <>
                  <Select
                    value={comparisonStudyId || ''}
                    onChange={(e) => setComparisonStudyId(e.target.value as number)}
                    size="small"
                    sx={{ position: 'absolute', top: 8, right: 8, zIndex: 10, minWidth: 150 }}
                  >
                    {priorStudies.map((ps) => (
                      <MenuItem key={ps.id} value={ps.id}>
                        {ps.studyDate?.split('T')[0]} - {ps.studyDescription}
                      </MenuItem>
                    ))}
                  </Select>
                  <Chip label="Prior" size="small" sx={{ position: 'absolute', top: 8, left: 8 }} color="secondary" />
                </>
              ) : (
                <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'center', height: '100%', color: 'text.secondary' }}>
                  <Typography>No prior studies available</Typography>
                </Box>
              )}
            </Box>
          </Box>
        )}

        {/* Cine Controls */}
        {currentInstance && currentInstance.numberOfFrames > 1 && viewMode === 'single' && (
          <Paper sx={{ display: 'flex', alignItems: 'center', gap: 1, p: 1, borderRadius: 0, backgroundColor: '#1a1a1a', borderTop: '1px solid #333' }}>
            <IconButton size="small" onClick={() => setFrame(0)}><FirstPage /></IconButton>
            <IconButton size="small" onClick={() => setFrame(Math.max(0, viewer.currentFrame - 1))}><SkipPrevious /></IconButton>
            <IconButton size="small" onClick={() => setIsPlaying(!isPlaying)}>{isPlaying ? <Pause /> : <PlayArrow />}</IconButton>
            <IconButton size="small" onClick={() => setFrame(Math.min(currentInstance.numberOfFrames - 1, viewer.currentFrame + 1))}><SkipNext /></IconButton>
            <IconButton size="small" onClick={() => setFrame(currentInstance.numberOfFrames - 1)}><LastPage /></IconButton>
            <Slider value={viewer.currentFrame} onChange={(_, v) => setFrame(v as number)} min={0} max={currentInstance.numberOfFrames - 1} sx={{ mx: 2, flex: 1 }} />
            <Typography variant="caption" sx={{ minWidth: 60, textAlign: 'right' }}>{viewer.currentFrame + 1} / {currentInstance.numberOfFrames}</Typography>
          </Paper>
        )}
      </Box>

      {/* Right Panel - DICOM Tags / Measurements */}
      {showTags && currentInstance && <DicomTagsViewer instanceId={currentInstance.id} onClose={() => setShowTags(false)} />}
      {showMeasurementPanel && <MeasurementPanel onClose={() => setShowMeasurementPanel(false)} />}
      {showReportPanel && currentStudy && <ReportPanel studyId={currentStudy.id} onClose={() => setShowReportPanel(false)} />}

      {/* Image Enhancement Dialog */}
      <Dialog open={showEnhancementDialog} onClose={() => setShowEnhancementDialog(false)} maxWidth="sm" fullWidth>
        <DialogTitle>Image Enhancement</DialogTitle>
        <DialogContent>
          <Box sx={{ pt: 2 }}>
            <Typography gutterBottom>Sharpen</Typography>
            <Slider value={enhancementSettings.sharpen} onChange={(_, v) => setEnhancementSettings({ ...enhancementSettings, sharpen: v as number })} min={0} max={100} />
            
            <Typography gutterBottom>Smooth</Typography>
            <Slider value={enhancementSettings.smooth} onChange={(_, v) => setEnhancementSettings({ ...enhancementSettings, smooth: v as number })} min={0} max={100} />
            
            <Typography gutterBottom>Noise Reduction</Typography>
            <Slider value={enhancementSettings.noiseReduction} onChange={(_, v) => setEnhancementSettings({ ...enhancementSettings, noiseReduction: v as number })} min={0} max={100} />
            
            <Typography gutterBottom>Edge Enhancement</Typography>
            <Slider value={enhancementSettings.edgeEnhancement} onChange={(_, v) => setEnhancementSettings({ ...enhancementSettings, edgeEnhancement: v as number })} min={0} max={100} />
            
            <Divider sx={{ my: 2 }} />
            
            <Typography gutterBottom>Brightness</Typography>
            <Slider value={enhancementSettings.brightness} onChange={(_, v) => setEnhancementSettings({ ...enhancementSettings, brightness: v as number })} min={-100} max={100} />
            
            <Typography gutterBottom>Contrast</Typography>
            <Slider value={enhancementSettings.contrast} onChange={(_, v) => setEnhancementSettings({ ...enhancementSettings, contrast: v as number })} min={-100} max={100} />
            
            <Typography gutterBottom>Gamma: {enhancementSettings.gamma.toFixed(2)}</Typography>
            <Slider value={enhancementSettings.gamma} onChange={(_, v) => setEnhancementSettings({ ...enhancementSettings, gamma: v as number })} min={0.1} max={3} step={0.1} />
          </Box>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setEnhancementSettings({ sharpen: 0, smooth: 0, noiseReduction: 0, edgeEnhancement: 0, brightness: 0, contrast: 0, gamma: 1.0 })}>Reset</Button>
          <Button onClick={() => setShowEnhancementDialog(false)}>Close</Button>
          <Button variant="contained" onClick={() => setShowEnhancementDialog(false)}>Apply</Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
};

export default AdvancedViewer;
