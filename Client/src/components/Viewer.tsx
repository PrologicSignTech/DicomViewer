import React, { useEffect, useRef, useState } from 'react';
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
} from '@mui/icons-material';
import useAppStore, { windowPresets } from '../services/store';
import apiService from '../services/api';
import SeriesPanel from './SeriesPanel';
import DicomTagsViewer from './DicomTagsViewer';
import { ToolType } from '../types';

const Viewer: React.FC = () => {
  const { studyId } = useParams();
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

  const [imageUrl, setImageUrl] = useState<string>('');
  const [isPlaying, setIsPlaying] = useState(false);
  const [presetAnchor, setPresetAnchor] = useState<null | HTMLElement>(null);
  const [layoutAnchor, setLayoutAnchor] = useState<null | HTMLElement>(null);
  const [showTags, setShowTags] = useState(false);
  const [mouseDown, setMouseDown] = useState(false);
  const [lastMousePos, setLastMousePos] = useState({ x: 0, y: 0 });
  const [pixelValue, setPixelValue] = useState<{ x: number; y: number; value: number; unit: string } | null>(null);

  useEffect(() => {
    if (studyId) {
      selectStudy(parseInt(studyId));
    }
  }, [studyId]);

  useEffect(() => {
    if (currentInstance) {
      const url = apiService.getImageUrl(
        currentInstance.id,
        viewer.currentFrame,
        viewer.windowCenter,
        viewer.windowWidth,
        viewer.invert
      );
      setImageUrl(url);
    }
  }, [currentInstance, viewer.currentFrame, viewer.windowCenter, viewer.windowWidth, viewer.invert]);

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

  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
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
      }
    };

    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [viewer.currentFrame, isPlaying, currentInstance]);

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

  const handleMouseUp = () => {
    setMouseDown(false);
  };

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
    { id: 'wwwc', icon: <Contrast />, label: 'Window/Level' },
    { id: 'pan', icon: <PanTool />, label: 'Pan' },
    { id: 'zoom', icon: <ZoomIn />, label: 'Zoom' },
    { id: 'length', icon: <Straighten />, label: 'Length' },
    { id: 'angle', icon: <ChangeHistory />, label: 'Angle' },
    { id: 'ellipseRoi', icon: <RadioButtonUnchecked />, label: 'Ellipse ROI' },
    { id: 'rectangleRoi', icon: <Circle />, label: 'Rectangle ROI' },
    { id: 'probe', icon: <Info />, label: 'Probe' },
    { id: 'text', icon: <TextFields />, label: 'Text' },
    { id: 'arrow', icon: <ArrowForward />, label: 'Arrow' },
  ];

  const imageTransform = `
    translate(${viewer.pan.x}px, ${viewer.pan.y}px)
    scale(${viewer.zoom})
    rotate(${viewer.rotation}deg)
    scaleX(${viewer.flipH ? -1 : 1})
    scaleY(${viewer.flipV ? -1 : 1})
  `;

  return (
    <Box sx={{ display: 'flex', height: 'calc(100vh - 48px)', overflow: 'hidden' }}>
      <SeriesPanel />

      <Box sx={{ flex: 1, display: 'flex', flexDirection: 'column' }}>
        <Paper
          sx={{
            display: 'flex',
            alignItems: 'center',
            gap: 0.5,
            p: 0.5,
            borderRadius: 0,
            backgroundColor: '#1a1a1a',
            borderBottom: '1px solid #333',
          }}
        >
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

          <Tooltip title="Window Presets">
            <IconButton size="small" onClick={(e) => setPresetAnchor(e.currentTarget)}>
              <Brightness6 />
            </IconButton>
          </Tooltip>
          <Menu
            anchorEl={presetAnchor}
            open={Boolean(presetAnchor)}
            onClose={() => setPresetAnchor(null)}
          >
            {windowPresets.map((preset) => (
              <MenuItem
                key={preset.name}
                onClick={() => {
                  applyPreset(preset);
                  setPresetAnchor(null);
                }}
              >
                <ListItemText primary={preset.name} secondary={`WC: ${preset.windowCenter} WW: ${preset.windowWidth}`} />
              </MenuItem>
            ))}
          </Menu>

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

          <Divider orientation="vertical" flexItem sx={{ mx: 0.5 }} />

          <Tooltip title="Reset (R)">
            <IconButton size="small" onClick={resetViewer}>
              <Refresh />
            </IconButton>
          </Tooltip>

          <Tooltip title="Layout">
            <IconButton size="small" onClick={(e) => setLayoutAnchor(e.currentTarget)}>
              <GridOn />
            </IconButton>
          </Tooltip>
          <Menu
            anchorEl={layoutAnchor}
            open={Boolean(layoutAnchor)}
            onClose={() => setLayoutAnchor(null)}
          >
            {['1x1', '1x2', '2x1', '2x2'].map((layout) => (
              <MenuItem key={layout} onClick={() => setLayoutAnchor(null)}>
                {layout}
              </MenuItem>
            ))}
          </Menu>

          <Tooltip title="DICOM Tags">
            <IconButton size="small" onClick={() => setShowTags(!showTags)} color={showTags ? 'primary' : 'default'}>
              <Info />
            </IconButton>
          </Tooltip>

          <Box sx={{ flexGrow: 1 }} />

          <Typography variant="caption" sx={{ mr: 2 }}>
            WC: {Math.round(viewer.windowCenter)} | WW: {Math.round(viewer.windowWidth)}
          </Typography>
        </Paper>

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

              {viewer.showOverlay && (
                <>
                  <Box
                    sx={{
                      position: 'absolute',
                      top: 8,
                      left: 8,
                      color: '#fff',
                      textShadow: '1px 1px 2px #000',
                      fontSize: '12px',
                      fontFamily: 'monospace',
                    }}
                  >
                    <div>{currentStudy?.patientName || 'Unknown'}</div>
                    <div>ID: {currentStudy?.patientId || '-'}</div>
                    <div>{currentStudy?.studyDescription || ''}</div>
                  </Box>

                  <Box
                    sx={{
                      position: 'absolute',
                      top: 8,
                      right: 8,
                      color: '#fff',
                      textShadow: '1px 1px 2px #000',
                      fontSize: '12px',
                      fontFamily: 'monospace',
                      textAlign: 'right',
                    }}
                  >
                    <div>{currentSeries?.modality} - {currentSeries?.seriesDescription || ''}</div>
                    <div>{currentInstance.columns} x {currentInstance.rows}</div>
                  </Box>

                  <Box
                    sx={{
                      position: 'absolute',
                      bottom: 8,
                      left: 8,
                      color: '#fff',
                      textShadow: '1px 1px 2px #000',
                      fontSize: '12px',
                      fontFamily: 'monospace',
                    }}
                  >
                    <div>WC: {Math.round(viewer.windowCenter)} WW: {Math.round(viewer.windowWidth)}</div>
                    <div>Zoom: {(viewer.zoom * 100).toFixed(0)}%</div>
                    {pixelValue && (
                      <div>Pixel ({pixelValue.x}, {pixelValue.y}): {pixelValue.value.toFixed(1)} {pixelValue.unit}</div>
                    )}
                  </Box>

                  <Box
                    sx={{
                      position: 'absolute',
                      bottom: 8,
                      right: 8,
                      color: '#fff',
                      textShadow: '1px 1px 2px #000',
                      fontSize: '12px',
                      fontFamily: 'monospace',
                      textAlign: 'right',
                    }}
                  >
                    {currentInstance.numberOfFrames > 1 && (
                      <div>Frame: {viewer.currentFrame + 1} / {currentInstance.numberOfFrames}</div>
                    )}
                    {currentSeries && (
                      <div>
                        Image: {currentSeries.instances.findIndex(i => i.id === currentInstance.id) + 1} / {currentSeries.instances.length}
                      </div>
                    )}
                  </Box>
                </>
              )}
            </>
          ) : (
            <Box
              sx={{
                position: 'absolute',
                top: '50%',
                left: '50%',
                transform: 'translate(-50%, -50%)',
                textAlign: 'center',
                color: 'text.secondary',
              }}
            >
              <Typography variant="h6">No image selected</Typography>
              <Typography variant="body2">Select a study from the list to view</Typography>
            </Box>
          )}
        </Box>

        {currentInstance && currentInstance.numberOfFrames > 1 && (
          <Paper
            sx={{
              display: 'flex',
              alignItems: 'center',
              gap: 1,
              p: 1,
              borderRadius: 0,
              backgroundColor: '#1a1a1a',
              borderTop: '1px solid #333',
            }}
          >
            <IconButton size="small" onClick={() => setFrame(0)}>
              <FirstPage />
            </IconButton>
            <IconButton size="small" onClick={() => setFrame(Math.max(0, viewer.currentFrame - 1))}>
              <SkipPrevious />
            </IconButton>
            <IconButton size="small" onClick={() => setIsPlaying(!isPlaying)}>
              {isPlaying ? <Pause /> : <PlayArrow />}
            </IconButton>
            <IconButton size="small" onClick={() => setFrame(Math.min(currentInstance.numberOfFrames - 1, viewer.currentFrame + 1))}>
              <SkipNext />
            </IconButton>
            <IconButton size="small" onClick={() => setFrame(currentInstance.numberOfFrames - 1)}>
              <LastPage />
            </IconButton>

            <Slider
              value={viewer.currentFrame}
              onChange={(_, value) => setFrame(value as number)}
              min={0}
              max={currentInstance.numberOfFrames - 1}
              sx={{ mx: 2, flex: 1 }}
            />

            <Typography variant="caption" sx={{ minWidth: 60, textAlign: 'right' }}>
              {viewer.currentFrame + 1} / {currentInstance.numberOfFrames}
            </Typography>
          </Paper>
        )}
      </Box>

      {showTags && currentInstance && (
        <DicomTagsViewer instanceId={currentInstance.id} onClose={() => setShowTags(false)} />
      )}
    </Box>
  );
};

export default Viewer;
