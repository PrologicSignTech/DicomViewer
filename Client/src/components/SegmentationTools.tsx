import React, { useState } from 'react';
import {
  Box,
  Paper,
  Typography,
  IconButton,
  Tooltip,
  Slider,
  ToggleButton,
  ToggleButtonGroup,
  Divider,
  TextField,
  Button,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  List,
  ListItem,
  ListItemText,
  ListItemSecondaryAction,
  Chip,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
} from '@mui/material';
import {
  Brush,
  Delete,
  Undo,
  Redo,
  Visibility,
  VisibilityOff,
  Save,
  FolderOpen,
  FormatPaint,
  AutoFixHigh,
  ContentCut,
  CallMerge,
  ColorLens,
  Layers,
  Add as AddIcon,
} from '@mui/icons-material';

interface SegmentationLabel {
  id: number;
  name: string;
  color: string;
  visible: boolean;
}

interface SegmentationToolsProps {
  onToolChange?: (tool: string) => void;
  onBrushSizeChange?: (size: number) => void;
  onLabelChange?: (labelId: number) => void;
}

const SegmentationTools: React.FC<SegmentationToolsProps> = ({
  onToolChange,
  onBrushSizeChange,
  onLabelChange,
}) => {
  const [activeTool, setActiveTool] = useState<string>('brush');
  const [brushSize, setBrushSize] = useState<number>(10);
  const [labels, setLabels] = useState<SegmentationLabel[]>([
    { id: 1, name: 'Tumor', color: '#ff4444', visible: true },
    { id: 2, name: 'Organ', color: '#44ff44', visible: true },
    { id: 3, name: 'Vessel', color: '#4444ff', visible: true },
  ]);
  const [activeLabel, setActiveLabel] = useState<number>(1);
  const [showLabelDialog, setShowLabelDialog] = useState(false);
  const [newLabelName, setNewLabelName] = useState('');
  const [newLabelColor, setNewLabelColor] = useState('#ffffff');
  const [opacity, setOpacity] = useState<number>(50);
  const [thresholdMin, setThresholdMin] = useState<number>(0);
  const [thresholdMax, setThresholdMax] = useState<number>(255);

  const handleToolChange = (tool: string) => {
    setActiveTool(tool);
    onToolChange?.(tool);
  };

  const handleBrushSizeChange = (_: Event, value: number | number[]) => {
    const size = value as number;
    setBrushSize(size);
    onBrushSizeChange?.(size);
  };

  const handleLabelChange = (labelId: number) => {
    setActiveLabel(labelId);
    onLabelChange?.(labelId);
  };

  const toggleLabelVisibility = (labelId: number) => {
    setLabels(
      labels.map((label) =>
        label.id === labelId ? { ...label, visible: !label.visible } : label
      )
    );
  };

  const addNewLabel = () => {
    const newLabel: SegmentationLabel = {
      id: labels.length + 1,
      name: newLabelName,
      color: newLabelColor,
      visible: true,
    };
    setLabels([...labels, newLabel]);
    setNewLabelName('');
    setNewLabelColor('#ffffff');
    setShowLabelDialog(false);
  };

  const deleteLabel = (labelId: number) => {
    setLabels(labels.filter((label) => label.id !== labelId));
    if (activeLabel === labelId) {
      setActiveLabel(labels[0]?.id || 1);
    }
  };

  const predefinedColors = [
    '#ff4444',
    '#44ff44',
    '#4444ff',
    '#ffff44',
    '#ff44ff',
    '#44ffff',
    '#ff8844',
    '#88ff44',
    '#8844ff',
  ];

  return (
    <Box sx={{ width: '100%', height: '100%', p: 2 }}>
      <Typography variant="h6" sx={{ mb: 2, fontWeight: 600 }}>
        Segmentation Tools
      </Typography>

      {/* Tool Selection */}
      <Paper sx={{ p: 2, mb: 2, backgroundColor: '#1a1a1a' }}>
        <Typography variant="subtitle2" sx={{ mb: 1 }}>
          Drawing Tools
        </Typography>
        <ToggleButtonGroup
          value={activeTool}
          exclusive
          onChange={(_, value) => value && handleToolChange(value)}
          fullWidth
          size="small"
        >
          <ToggleButton value="brush">
            <Tooltip title="Brush">
              <Brush />
            </Tooltip>
          </ToggleButton>
          <ToggleButton value="eraser">
            <Tooltip title="Eraser">
              <Delete />
            </Tooltip>
          </ToggleButton>
          <ToggleButton value="fill">
            <Tooltip title="Fill">
              <FormatPaint />
            </Tooltip>
          </ToggleButton>
          <ToggleButton value="threshold">
            <Tooltip title="Threshold">
              <AutoFixHigh />
            </Tooltip>
          </ToggleButton>
        </ToggleButtonGroup>

        {/* Brush Size */}
        <Box sx={{ mt: 2 }}>
          <Typography variant="caption" gutterBottom>
            Brush Size: {brushSize}px
          </Typography>
          <Slider
            value={brushSize}
            onChange={handleBrushSizeChange}
            min={1}
            max={50}
            sx={{ color: '#4fc3f7' }}
          />
        </Box>

        {/* Opacity */}
        <Box sx={{ mt: 2 }}>
          <Typography variant="caption" gutterBottom>
            Overlay Opacity: {opacity}%
          </Typography>
          <Slider
            value={opacity}
            onChange={(_, value) => setOpacity(value as number)}
            min={0}
            max={100}
            sx={{ color: '#4fc3f7' }}
          />
        </Box>
      </Paper>

      {/* Threshold Settings (shown when threshold tool is active) */}
      {activeTool === 'threshold' && (
        <Paper sx={{ p: 2, mb: 2, backgroundColor: '#1a1a1a' }}>
          <Typography variant="subtitle2" sx={{ mb: 1 }}>
            Threshold Range
          </Typography>
          <Box sx={{ display: 'flex', gap: 2, mb: 1 }}>
            <TextField
              label="Min"
              type="number"
              size="small"
              value={thresholdMin}
              onChange={(e) => setThresholdMin(parseInt(e.target.value))}
              fullWidth
            />
            <TextField
              label="Max"
              type="number"
              size="small"
              value={thresholdMax}
              onChange={(e) => setThresholdMax(parseInt(e.target.value))}
              fullWidth
            />
          </Box>
          <Button variant="outlined" size="small" fullWidth>
            Apply Threshold
          </Button>
        </Paper>
      )}

      {/* Labels */}
      <Paper sx={{ p: 2, mb: 2, backgroundColor: '#1a1a1a' }}>
        <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', mb: 1 }}>
          <Typography variant="subtitle2">Segmentation Labels</Typography>
          <IconButton size="small" onClick={() => setShowLabelDialog(true)}>
            <AddIcon />
          </IconButton>
        </Box>
        <List dense>
          {labels.map((label) => (
            <ListItem
              key={label.id}
              button
              selected={activeLabel === label.id}
              onClick={() => handleLabelChange(label.id)}
              sx={{
                borderLeft: `4px solid ${label.color}`,
                mb: 0.5,
                borderRadius: 1,
                backgroundColor:
                  activeLabel === label.id ? 'rgba(79, 195, 247, 0.1)' : 'transparent',
              }}
            >
              <ListItemText
                primary={label.name}
                secondary={
                  <Chip
                    label={`ID: ${label.id}`}
                    size="small"
                    sx={{
                      backgroundColor: label.color,
                      color: '#000',
                      height: 16,
                      fontSize: '0.7rem',
                    }}
                  />
                }
              />
              <ListItemSecondaryAction>
                <IconButton
                  edge="end"
                  size="small"
                  onClick={() => toggleLabelVisibility(label.id)}
                >
                  {label.visible ? <Visibility /> : <VisibilityOff />}
                </IconButton>
                <IconButton edge="end" size="small" onClick={() => deleteLabel(label.id)}>
                  <Delete />
                </IconButton>
              </ListItemSecondaryAction>
            </ListItem>
          ))}
        </List>
      </Paper>

      {/* Advanced Tools */}
      <Paper sx={{ p: 2, mb: 2, backgroundColor: '#1a1a1a' }}>
        <Typography variant="subtitle2" sx={{ mb: 1 }}>
          Advanced Operations
        </Typography>
        <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1 }}>
          <Button variant="outlined" size="small" startIcon={<ContentCut />}>
            Split Label
          </Button>
          <Button variant="outlined" size="small" startIcon={<CallMerge />}>
            Merge Labels
          </Button>
          <Button variant="outlined" size="small" startIcon={<AutoFixHigh />}>
            Interpolate Slices
          </Button>
        </Box>
      </Paper>

      {/* Actions */}
      <Paper sx={{ p: 2, backgroundColor: '#1a1a1a' }}>
        <Typography variant="subtitle2" sx={{ mb: 1 }}>
          Actions
        </Typography>
        <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1 }}>
          <Box sx={{ display: 'flex', gap: 1 }}>
            <IconButton size="small">
              <Undo />
            </IconButton>
            <IconButton size="small">
              <Redo />
            </IconButton>
          </Box>
          <Divider sx={{ my: 1 }} />
          <Button variant="contained" size="small" startIcon={<Save />} fullWidth>
            Export DICOM-SEG
          </Button>
          <Button variant="outlined" size="small" startIcon={<FolderOpen />} fullWidth>
            Load Segmentation
          </Button>
        </Box>
      </Paper>

      {/* Add Label Dialog */}
      <Dialog open={showLabelDialog} onClose={() => setShowLabelDialog(false)} maxWidth="xs" fullWidth>
        <DialogTitle>Add New Label</DialogTitle>
        <DialogContent>
          <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2, pt: 1 }}>
            <TextField
              label="Label Name"
              value={newLabelName}
              onChange={(e) => setNewLabelName(e.target.value)}
              fullWidth
            />
            <Box>
              <Typography variant="caption" gutterBottom>
                Select Color
              </Typography>
              <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1, mt: 1 }}>
                {predefinedColors.map((color) => (
                  <Box
                    key={color}
                    onClick={() => setNewLabelColor(color)}
                    sx={{
                      width: 40,
                      height: 40,
                      backgroundColor: color,
                      border: newLabelColor === color ? '3px solid #fff' : '1px solid #555',
                      borderRadius: 1,
                      cursor: 'pointer',
                      '&:hover': {
                        border: '3px solid #aaa',
                      },
                    }}
                  />
                ))}
              </Box>
            </Box>
          </Box>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setShowLabelDialog(false)}>Cancel</Button>
          <Button variant="contained" onClick={addNewLabel} disabled={!newLabelName}>
            Add Label
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
};

export default SegmentationTools;
