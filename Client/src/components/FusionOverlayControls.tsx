import React, { useState } from 'react';
import {
  Box,
  Paper,
  Typography,
  Slider,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  Button,
  IconButton,
  Tooltip,
  Switch,
  FormControlLabel,
  ToggleButtonGroup,
  ToggleButton,
  Divider,
  Chip,
} from '@mui/material';
import {
  Layers,
  Palette,
  Tune,
  Visibility,
  VisibilityOff,
  SwapHoriz,
  SyncAlt,
  CompareArrows,
  Straighten,
  RotateRight,
  ZoomIn,
} from '@mui/icons-material';

interface FusionLayer {
  id: string;
  name: string;
  modality: string;
  visible: boolean;
  opacity: number;
  colorMap?: string;
}

interface FusionOverlayControlsProps {
  onOpacityChange?: (layerId: string, opacity: number) => void;
  onColorMapChange?: (layerId: string, colorMap: string) => void;
  onLayerToggle?: (layerId: string, visible: boolean) => void;
  onBlendModeChange?: (mode: string) => void;
  onAlignmentChange?: (adjustments: AlignmentAdjustments) => void;
}

interface AlignmentAdjustments {
  translateX: number;
  translateY: number;
  translateZ: number;
  rotateX: number;
  rotateY: number;
  rotateZ: number;
  scale: number;
}

const FusionOverlayControls: React.FC<FusionOverlayControlsProps> = ({
  onOpacityChange,
  onColorMapChange,
  onLayerToggle,
  onBlendModeChange,
  onAlignmentChange,
}) => {
  const [layers, setLayers] = useState<FusionLayer[]>([
    { id: 'ct', name: 'CT Scan', modality: 'CT', visible: true, opacity: 100 },
    { id: 'pet', name: 'PET Scan', modality: 'PT', visible: true, opacity: 50, colorMap: 'hot' },
  ]);

  const [blendMode, setBlendMode] = useState<string>('overlay');
  const [syncNavigation, setSyncNavigation] = useState(true);
  const [showAlignment, setShowAlignment] = useState(false);

  const [alignment, setAlignment] = useState<AlignmentAdjustments>({
    translateX: 0,
    translateY: 0,
    translateZ: 0,
    rotateX: 0,
    rotateY: 0,
    rotateZ: 0,
    scale: 1,
  });

  const colorMaps = [
    { value: 'gray', label: 'Grayscale' },
    { value: 'hot', label: 'Hot' },
    { value: 'cool', label: 'Cool' },
    { value: 'jet', label: 'Jet' },
    { value: 'rainbow', label: 'Rainbow' },
    { value: 'viridis', label: 'Viridis' },
    { value: 'plasma', label: 'Plasma' },
    { value: 'pet', label: 'PET' },
  ];

  const blendModes = [
    { value: 'overlay', label: 'Overlay' },
    { value: 'multiply', label: 'Multiply' },
    { value: 'screen', label: 'Screen' },
    { value: 'add', label: 'Add' },
    { value: 'subtract', label: 'Subtract' },
    { value: 'difference', label: 'Difference' },
  ];

  const handleOpacityChange = (layerId: string, opacity: number) => {
    setLayers(
      layers.map((layer) =>
        layer.id === layerId ? { ...layer, opacity } : layer
      )
    );
    onOpacityChange?.(layerId, opacity);
  };

  const handleColorMapChange = (layerId: string, colorMap: string) => {
    setLayers(
      layers.map((layer) =>
        layer.id === layerId ? { ...layer, colorMap } : layer
      )
    );
    onColorMapChange?.(layerId, colorMap);
  };

  const handleLayerToggle = (layerId: string) => {
    setLayers(
      layers.map((layer) =>
        layer.id === layerId ? { ...layer, visible: !layer.visible } : layer
      )
    );
    const layer = layers.find((l) => l.id === layerId);
    if (layer) {
      onLayerToggle?.(layerId, !layer.visible);
    }
  };

  const handleBlendModeChange = (mode: string) => {
    setBlendMode(mode);
    onBlendModeChange?.(mode);
  };

  const handleAlignmentChange = (key: keyof AlignmentAdjustments, value: number) => {
    const newAlignment = { ...alignment, [key]: value };
    setAlignment(newAlignment);
    onAlignmentChange?.(newAlignment);
  };

  const resetAlignment = () => {
    const defaultAlignment: AlignmentAdjustments = {
      translateX: 0,
      translateY: 0,
      translateZ: 0,
      rotateX: 0,
      rotateY: 0,
      rotateZ: 0,
      scale: 1,
    };
    setAlignment(defaultAlignment);
    onAlignmentChange?.(defaultAlignment);
  };

  return (
    <Box sx={{ width: '100%', p: 2 }}>
      <Typography variant="h6" sx={{ mb: 2, fontWeight: 600 }}>
        Multimodality Fusion
      </Typography>

      {/* Fusion Layers */}
      <Paper sx={{ p: 2, mb: 2, backgroundColor: '#1a1a1a' }}>
        <Typography variant="subtitle2" sx={{ mb: 2 }}>
          Fusion Layers
        </Typography>

        {layers.map((layer) => (
          <Box key={layer.id} sx={{ mb: 2 }}>
            <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', mb: 1 }}>
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                <Chip
                  label={layer.modality}
                  size="small"
                  sx={{ backgroundColor: '#4fc3f7', color: '#000', fontWeight: 600 }}
                />
                <Typography variant="body2">{layer.name}</Typography>
              </Box>
              <IconButton size="small" onClick={() => handleLayerToggle(layer.id)}>
                {layer.visible ? <Visibility /> : <VisibilityOff />}
              </IconButton>
            </Box>

            {layer.visible && (
              <>
                <Typography variant="caption" gutterBottom>
                  Opacity: {layer.opacity}%
                </Typography>
                <Slider
                  value={layer.opacity}
                  onChange={(_, value) => handleOpacityChange(layer.id, value as number)}
                  min={0}
                  max={100}
                  sx={{ color: '#4fc3f7', mb: 1 }}
                />

                {layer.modality === 'PT' && (
                  <FormControl size="small" fullWidth>
                    <InputLabel>Color Map</InputLabel>
                    <Select
                      value={layer.colorMap || 'hot'}
                      label="Color Map"
                      onChange={(e) => handleColorMapChange(layer.id, e.target.value)}
                    >
                      {colorMaps.map((cm) => (
                        <MenuItem key={cm.value} value={cm.value}>
                          {cm.label}
                        </MenuItem>
                      ))}
                    </Select>
                  </FormControl>
                )}
              </>
            )}

            <Divider sx={{ mt: 2 }} />
          </Box>
        ))}
      </Paper>

      {/* Blend Mode */}
      <Paper sx={{ p: 2, mb: 2, backgroundColor: '#1a1a1a' }}>
        <Typography variant="subtitle2" sx={{ mb: 1 }}>
          Blend Mode
        </Typography>
        <FormControl size="small" fullWidth>
          <Select
            value={blendMode}
            onChange={(e) => handleBlendModeChange(e.target.value)}
          >
            {blendModes.map((mode) => (
              <MenuItem key={mode.value} value={mode.value}>
                {mode.label}
              </MenuItem>
            ))}
          </Select>
        </FormControl>
      </Paper>

      {/* Synchronization */}
      <Paper sx={{ p: 2, mb: 2, backgroundColor: '#1a1a1a' }}>
        <Typography variant="subtitle2" sx={{ mb: 1 }}>
          Synchronization
        </Typography>
        <FormControlLabel
          control={
            <Switch
              checked={syncNavigation}
              onChange={(e) => setSyncNavigation(e.target.checked)}
              color="primary"
            />
          }
          label="Sync Navigation"
        />
        <Typography variant="caption" color="text.secondary" display="block">
          Synchronized zoom, pan, and slice navigation
        </Typography>
      </Paper>

      {/* Manual Registration */}
      <Paper sx={{ p: 2, mb: 2, backgroundColor: '#1a1a1a' }}>
        <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', mb: 1 }}>
          <Typography variant="subtitle2">Manual Registration</Typography>
          <IconButton size="small" onClick={() => setShowAlignment(!showAlignment)}>
            <Tune />
          </IconButton>
        </Box>

        {showAlignment && (
          <Box sx={{ pt: 2 }}>
            {/* Translation Controls */}
            <Typography variant="caption" gutterBottom display="block">
              Translation
            </Typography>
            <Box sx={{ mb: 2 }}>
              <Typography variant="caption">X: {alignment.translateX}px</Typography>
              <Slider
                value={alignment.translateX}
                onChange={(_, value) => handleAlignmentChange('translateX', value as number)}
                min={-50}
                max={50}
                sx={{ color: '#4fc3f7' }}
              />
              <Typography variant="caption">Y: {alignment.translateY}px</Typography>
              <Slider
                value={alignment.translateY}
                onChange={(_, value) => handleAlignmentChange('translateY', value as number)}
                min={-50}
                max={50}
                sx={{ color: '#4fc3f7' }}
              />
              <Typography variant="caption">Z: {alignment.translateZ}px</Typography>
              <Slider
                value={alignment.translateZ}
                onChange={(_, value) => handleAlignmentChange('translateZ', value as number)}
                min={-50}
                max={50}
                sx={{ color: '#4fc3f7' }}
              />
            </Box>

            {/* Rotation Controls */}
            <Typography variant="caption" gutterBottom display="block">
              Rotation
            </Typography>
            <Box sx={{ mb: 2 }}>
              <Typography variant="caption">X: {alignment.rotateX}°</Typography>
              <Slider
                value={alignment.rotateX}
                onChange={(_, value) => handleAlignmentChange('rotateX', value as number)}
                min={-180}
                max={180}
                sx={{ color: '#4fc3f7' }}
              />
              <Typography variant="caption">Y: {alignment.rotateY}°</Typography>
              <Slider
                value={alignment.rotateY}
                onChange={(_, value) => handleAlignmentChange('rotateY', value as number)}
                min={-180}
                max={180}
                sx={{ color: '#4fc3f7' }}
              />
              <Typography variant="caption">Z: {alignment.rotateZ}°</Typography>
              <Slider
                value={alignment.rotateZ}
                onChange={(_, value) => handleAlignmentChange('rotateZ', value as number)}
                min={-180}
                max={180}
                sx={{ color: '#4fc3f7' }}
              />
            </Box>

            {/* Scale Control */}
            <Typography variant="caption" gutterBottom display="block">
              Scale: {alignment.scale.toFixed(2)}
            </Typography>
            <Slider
              value={alignment.scale}
              onChange={(_, value) => handleAlignmentChange('scale', value as number)}
              min={0.5}
              max={2}
              step={0.01}
              sx={{ color: '#4fc3f7', mb: 2 }}
            />

            <Button variant="outlined" size="small" fullWidth onClick={resetAlignment}>
              Reset Alignment
            </Button>
          </Box>
        )}
      </Paper>

      {/* Preset Configurations */}
      <Paper sx={{ p: 2, backgroundColor: '#1a1a1a' }}>
        <Typography variant="subtitle2" sx={{ mb: 1 }}>
          Fusion Presets
        </Typography>
        <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1 }}>
          <Button variant="outlined" size="small">
            CT-PET Fusion
          </Button>
          <Button variant="outlined" size="small">
            MR-PET Fusion
          </Button>
          <Button variant="outlined" size="small">
            CT-MR Fusion
          </Button>
        </Box>
      </Paper>
    </Box>
  );
};

export default FusionOverlayControls;
