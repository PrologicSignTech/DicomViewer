import React, { useState } from 'react';
import {
  Box,
  Paper,
  Typography,
  IconButton,
  List,
  ListItem,
  ListItemText,
  ListItemSecondaryAction,
  Divider,
  Chip,
  Tabs,
  Tab,
  Button,
  Collapse,
  Tooltip,
} from '@mui/material';
import {
  Close,
  Delete,
  Visibility,
  VisibilityOff,
  ExpandMore,
  ExpandLess,
  Download,
  Assessment,
  Straighten,
  ChangeHistory,
  RadioButtonUnchecked,
  CropSquare,
} from '@mui/icons-material';
import useAppStore from '../services/store';

interface MeasurementPanelProps {
  onClose: () => void;
}

const MeasurementPanel: React.FC<MeasurementPanelProps> = ({ onClose }) => {
  const { measurements, annotations, removeMeasurement, removeAnnotation } = useAppStore();
  const [activeTab, setActiveTab] = useState(0);
  const [expandedId, setExpandedId] = useState<number | null>(null);

  const getMeasurementIcon = (type: string) => {
    switch (type) {
      case 'length': return <Straighten />;
      case 'angle': return <ChangeHistory />;
      case 'ellipseRoi': return <RadioButtonUnchecked />;
      case 'rectangleRoi': return <CropSquare />;
      default: return <Assessment />;
    }
  };

  const getMeasurementColor = (type: string) => {
    switch (type) {
      case 'length': return '#4fc3f7';
      case 'angle': return '#81c784';
      case 'ellipseRoi': return '#ffb74d';
      case 'rectangleRoi': return '#f48fb1';
      default: return '#9e9e9e';
    }
  };

  const formatValue = (value: number | undefined, unit: string | undefined) => {
    if (value === undefined) return '-';
    return `${value.toFixed(2)} ${unit || ''}`;
  };

  const exportMeasurements = () => {
    const data = {
      measurements: measurements.map(m => ({
        type: m.type,
        label: m.label,
        value: m.value,
        unit: m.unit,
        mean: m.mean,
        stdDev: m.stdDev,
        area: m.area,
      })),
      exportedAt: new Date().toISOString(),
    };
    
    const blob = new Blob([JSON.stringify(data, null, 2)], { type: 'application/json' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = 'measurements.json';
    a.click();
    URL.revokeObjectURL(url);
  };

  return (
    <Paper
      sx={{
        width: 350,
        display: 'flex',
        flexDirection: 'column',
        borderLeft: '1px solid #333',
        backgroundColor: '#151515',
      }}
    >
      {/* Header */}
      <Box sx={{ display: 'flex', alignItems: 'center', p: 1.5, borderBottom: '1px solid #333' }}>
        <Assessment sx={{ mr: 1, color: '#4fc3f7' }} />
        <Typography variant="subtitle1" sx={{ fontWeight: 600, flex: 1 }}>
          Measurements
        </Typography>
        <Tooltip title="Export">
          <IconButton size="small" onClick={exportMeasurements}>
            <Download />
          </IconButton>
        </Tooltip>
        <IconButton size="small" onClick={onClose}>
          <Close />
        </IconButton>
      </Box>

      {/* Tabs */}
      <Tabs value={activeTab} onChange={(_, v) => setActiveTab(v)} sx={{ borderBottom: '1px solid #333' }}>
        <Tab label={`Measurements (${measurements.length})`} />
        <Tab label={`Annotations (${annotations.length})`} />
      </Tabs>

      {/* Measurements List */}
      {activeTab === 0 && (
        <Box sx={{ flex: 1, overflow: 'auto' }}>
          {measurements.length === 0 ? (
            <Box sx={{ p: 3, textAlign: 'center' }}>
              <Assessment sx={{ fontSize: 48, color: 'text.secondary', mb: 1 }} />
              <Typography color="text.secondary">No measurements</Typography>
              <Typography variant="caption" color="text.secondary">
                Use measurement tools to add measurements
              </Typography>
            </Box>
          ) : (
            <List dense>
              {measurements.map((m) => (
                <React.Fragment key={m.id}>
                  <ListItem
                    button
                    onClick={() => setExpandedId(expandedId === m.id ? null : m.id)}
                    sx={{ '&:hover': { backgroundColor: 'rgba(79, 195, 247, 0.1)' } }}
                  >
                    <Box
                      sx={{
                        width: 4,
                        height: 40,
                        backgroundColor: getMeasurementColor(m.type),
                        borderRadius: 1,
                        mr: 1.5,
                      }}
                    />
                    <ListItemText
                      primary={
                        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                          {getMeasurementIcon(m.type)}
                          <Typography variant="body2">{m.label || m.type}</Typography>
                        </Box>
                      }
                      secondary={
                        <Typography variant="caption" color="text.secondary">
                          {formatValue(m.value, m.unit)}
                        </Typography>
                      }
                    />
                    <ListItemSecondaryAction>
                      <IconButton size="small" edge="end">
                        {expandedId === m.id ? <ExpandLess /> : <ExpandMore />}
                      </IconButton>
                    </ListItemSecondaryAction>
                  </ListItem>
                  
                  <Collapse in={expandedId === m.id}>
                    <Box sx={{ pl: 4, pr: 2, pb: 2, backgroundColor: 'rgba(0,0,0,0.2)' }}>
                      {/* ROI Statistics */}
                      {(m.type === 'ellipseRoi' || m.type === 'rectangleRoi') && (
                        <Box sx={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 1, mt: 1 }}>
                          <Box>
                            <Typography variant="caption" color="text.secondary">Mean</Typography>
                            <Typography variant="body2">{m.mean?.toFixed(2) || '-'} HU</Typography>
                          </Box>
                          <Box>
                            <Typography variant="caption" color="text.secondary">Std Dev</Typography>
                            <Typography variant="body2">{m.stdDev?.toFixed(2) || '-'}</Typography>
                          </Box>
                          <Box>
                            <Typography variant="caption" color="text.secondary">Min</Typography>
                            <Typography variant="body2">{m.min?.toFixed(2) || '-'} HU</Typography>
                          </Box>
                          <Box>
                            <Typography variant="caption" color="text.secondary">Max</Typography>
                            <Typography variant="body2">{m.max?.toFixed(2) || '-'} HU</Typography>
                          </Box>
                          <Box>
                            <Typography variant="caption" color="text.secondary">Area</Typography>
                            <Typography variant="body2">{m.area?.toFixed(2) || '-'} mm²</Typography>
                          </Box>
                        </Box>
                      )}
                      
                      <Box sx={{ display: 'flex', gap: 1, mt: 2 }}>
                        <Button size="small" startIcon={m.isVisible ? <VisibilityOff /> : <Visibility />}>
                          {m.isVisible ? 'Hide' : 'Show'}
                        </Button>
                        <Button size="small" color="error" startIcon={<Delete />} onClick={() => removeMeasurement(m.id)}>
                          Delete
                        </Button>
                      </Box>
                    </Box>
                  </Collapse>
                  <Divider />
                </React.Fragment>
              ))}
            </List>
          )}
        </Box>
      )}

      {/* Annotations List */}
      {activeTab === 1 && (
        <Box sx={{ flex: 1, overflow: 'auto' }}>
          {annotations.length === 0 ? (
            <Box sx={{ p: 3, textAlign: 'center' }}>
              <Typography color="text.secondary">No annotations</Typography>
            </Box>
          ) : (
            <List dense>
              {annotations.map((a) => (
                <ListItem key={a.id}>
                  <ListItemText
                    primary={a.type}
                    secondary={a.text || 'No text'}
                  />
                  <ListItemSecondaryAction>
                    <IconButton size="small" onClick={() => removeAnnotation(a.id)}>
                      <Delete />
                    </IconButton>
                  </ListItemSecondaryAction>
                </ListItem>
              ))}
            </List>
          )}
        </Box>
      )}

      {/* Summary */}
      <Box sx={{ p: 1.5, borderTop: '1px solid #333', backgroundColor: '#1a1a1a' }}>
        <Typography variant="caption" color="text.secondary">
          {measurements.length} measurements • {annotations.length} annotations
        </Typography>
      </Box>
    </Paper>
  );
};

export default MeasurementPanel;
