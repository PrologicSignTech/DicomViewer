import React, { useState, useEffect } from 'react';
import {
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Button,
  Grid,
  Card,
  CardContent,
  CardActionArea,
  Typography,
  Box,
  Chip,
  IconButton,
  Tooltip,
  TextField,
  Select,
  MenuItem,
  FormControl,
  InputLabel,
} from '@mui/material';
import {
  GridOn,
  ViewModule,
  ViewQuilt,
  Add as AddIcon,
  Edit as EditIcon,
  Delete as DeleteIcon,
  Save as SaveIcon,
} from '@mui/icons-material';
import apiService from '../services/api';

interface HangingProtocol {
  id?: number;
  name: string;
  modality?: string;
  bodyPart?: string;
  studyType?: string;
  layout: {
    rows: number;
    cols: number;
  };
  viewportSettings: ViewportSetting[];
  description?: string;
}

interface ViewportSetting {
  position: number;
  seriesFilter?: string;
  windowLevel?: { center: number; width: number };
  zoom?: number;
  orientation?: string;
}

interface HangingProtocolSelectorProps {
  open: boolean;
  onClose: () => void;
  onSelect: (protocol: HangingProtocol) => void;
  modality?: string;
  bodyPart?: string;
}

const HangingProtocolSelector: React.FC<HangingProtocolSelectorProps> = ({
  open,
  onClose,
  onSelect,
  modality,
  bodyPart,
}) => {
  const [protocols, setProtocols] = useState<HangingProtocol[]>([]);
  const [loading, setLoading] = useState(false);
  const [showCreateDialog, setShowCreateDialog] = useState(false);
  const [newProtocol, setNewProtocol] = useState<HangingProtocol>({
    name: '',
    layout: { rows: 1, cols: 1 },
    viewportSettings: [],
  });

  useEffect(() => {
    if (open) {
      loadProtocols();
    }
  }, [open, modality, bodyPart]);

  const loadProtocols = async () => {
    setLoading(true);
    try {
      const response = await fetch(
        `/api/workflow/hanging-protocols?${modality ? `modality=${modality}` : ''}${bodyPart ? `&bodyPart=${bodyPart}` : ''}`
      );
      const data = await response.json();
      setProtocols(data);
      
      // Add default protocols
      const defaultProtocols = getDefaultProtocols(modality);
      setProtocols([...defaultProtocols, ...data]);
    } catch (error) {
      console.error('Error loading protocols:', error);
    } finally {
      setLoading(false);
    }
  };

  const getDefaultProtocols = (modalityType?: string): HangingProtocol[] => {
    const defaults: HangingProtocol[] = [
      {
        name: 'Single View (1x1)',
        layout: { rows: 1, cols: 1 },
        viewportSettings: [{ position: 0 }],
        description: 'Single viewport for detailed viewing',
      },
      {
        name: 'Side-by-Side (1x2)',
        layout: { rows: 1, cols: 2 },
        viewportSettings: [{ position: 0 }, { position: 1 }],
        description: 'Compare two series or timepoints',
      },
      {
        name: 'Quad View (2x2)',
        layout: { rows: 2, cols: 2 },
        viewportSettings: [
          { position: 0 },
          { position: 1 },
          { position: 2 },
          { position: 3 },
        ],
        description: 'Four viewports for comprehensive analysis',
      },
      {
        name: 'Grid 3x3',
        layout: { rows: 3, cols: 3 },
        viewportSettings: Array.from({ length: 9 }, (_, i) => ({ position: i })),
        description: 'Nine viewports for multi-series comparison',
      },
    ];

    // Add modality-specific protocols
    if (modalityType === 'CT' || modalityType === 'MR') {
      defaults.push({
        name: 'MPR (Axial/Sagittal/Coronal)',
        layout: { rows: 2, cols: 2 },
        viewportSettings: [
          { position: 0, orientation: 'axial' },
          { position: 1, orientation: 'sagittal' },
          { position: 2, orientation: 'coronal' },
          { position: 3 },
        ],
        description: 'Multi-planar reconstruction layout',
        modality: modalityType,
      });
    }

    if (modalityType === 'CR' || modalityType === 'DX') {
      defaults.push({
        name: 'Chest PA/Lateral',
        layout: { rows: 1, cols: 2 },
        viewportSettings: [
          { position: 0, seriesFilter: 'PA' },
          { position: 1, seriesFilter: 'LAT' },
        ],
        description: 'Standard chest X-ray layout',
        modality: modalityType,
      });
    }

    return defaults;
  };

  const handleSelectProtocol = (protocol: HangingProtocol) => {
    onSelect(protocol);
    onClose();
  };

  const handleSaveProtocol = async () => {
    try {
      const response = await fetch('/api/workflow/hanging-protocols', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(newProtocol),
      });
      const saved = await response.json();
      setProtocols([...protocols, saved]);
      setShowCreateDialog(false);
      setNewProtocol({
        name: '',
        layout: { rows: 1, cols: 1 },
        viewportSettings: [],
      });
    } catch (error) {
      console.error('Error saving protocol:', error);
    }
  };

  const renderLayoutPreview = (layout: { rows: number; cols: number }) => {
    return (
      <Box
        sx={{
          display: 'grid',
          gridTemplateRows: `repeat(${layout.rows}, 1fr)`,
          gridTemplateColumns: `repeat(${layout.cols}, 1fr)`,
          gap: 0.5,
          width: 80,
          height: 80,
          border: '1px solid #555',
          borderRadius: 1,
          p: 0.5,
          backgroundColor: '#0a0a0a',
        }}
      >
        {Array.from({ length: layout.rows * layout.cols }).map((_, i) => (
          <Box
            key={i}
            sx={{
              backgroundColor: '#1a1a1a',
              border: '1px solid #4fc3f7',
              borderRadius: 0.5,
            }}
          />
        ))}
      </Box>
    );
  };

  return (
    <>
      <Dialog open={open} onClose={onClose} maxWidth="md" fullWidth>
        <DialogTitle>
          <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
            <Typography variant="h6">Select Hanging Protocol</Typography>
            <Button
              variant="outlined"
              startIcon={<AddIcon />}
              onClick={() => setShowCreateDialog(true)}
              size="small"
            >
              Create Custom
            </Button>
          </Box>
        </DialogTitle>
        <DialogContent>
          {modality && (
            <Chip
              label={`Modality: ${modality}`}
              sx={{ mr: 1, mb: 2 }}
              color="primary"
              size="small"
            />
          )}
          {bodyPart && (
            <Chip
              label={`Body Part: ${bodyPart}`}
              sx={{ mb: 2 }}
              color="secondary"
              size="small"
            />
          )}

          <Grid container spacing={2}>
            {protocols.map((protocol, index) => (
              <Grid item xs={12} sm={6} md={4} key={index}>
                <Card
                  sx={{
                    backgroundColor: '#1a1a1a',
                    border: '1px solid #333',
                    '&:hover': {
                      border: '1px solid #4fc3f7',
                      boxShadow: '0 0 10px rgba(79, 195, 247, 0.3)',
                    },
                  }}
                >
                  <CardActionArea onClick={() => handleSelectProtocol(protocol)}>
                    <CardContent>
                      <Box sx={{ display: 'flex', alignItems: 'center', mb: 1 }}>
                        {renderLayoutPreview(protocol.layout)}
                        <Box sx={{ ml: 2, flexGrow: 1 }}>
                          <Typography variant="subtitle2" sx={{ fontWeight: 600 }}>
                            {protocol.name}
                          </Typography>
                          <Typography variant="caption" color="text.secondary">
                            {protocol.layout.rows}×{protocol.layout.cols}
                          </Typography>
                        </Box>
                      </Box>
                      {protocol.description && (
                        <Typography variant="caption" color="text.secondary" display="block">
                          {protocol.description}
                        </Typography>
                      )}
                      {protocol.modality && (
                        <Chip
                          label={protocol.modality}
                          size="small"
                          sx={{ mt: 1, height: 20 }}
                        />
                      )}
                    </CardContent>
                  </CardActionArea>
                </Card>
              </Grid>
            ))}
          </Grid>
        </DialogContent>
        <DialogActions>
          <Button onClick={onClose}>Cancel</Button>
        </DialogActions>
      </Dialog>

      {/* Create Protocol Dialog */}
      <Dialog
        open={showCreateDialog}
        onClose={() => setShowCreateDialog(false)}
        maxWidth="sm"
        fullWidth
      >
        <DialogTitle>Create Custom Hanging Protocol</DialogTitle>
        <DialogContent>
          <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2, pt: 1 }}>
            <TextField
              label="Protocol Name"
              value={newProtocol.name}
              onChange={(e) => setNewProtocol({ ...newProtocol, name: e.target.value })}
              fullWidth
            />
            <TextField
              label="Description"
              value={newProtocol.description || ''}
              onChange={(e) => setNewProtocol({ ...newProtocol, description: e.target.value })}
              fullWidth
              multiline
              rows={2}
            />
            <FormControl fullWidth>
              <InputLabel>Modality</InputLabel>
              <Select
                value={newProtocol.modality || ''}
                label="Modality"
                onChange={(e) => setNewProtocol({ ...newProtocol, modality: e.target.value })}
              >
                <MenuItem value="">Any</MenuItem>
                <MenuItem value="CT">CT</MenuItem>
                <MenuItem value="MR">MR</MenuItem>
                <MenuItem value="CR">CR</MenuItem>
                <MenuItem value="DX">DX</MenuItem>
                <MenuItem value="US">US</MenuItem>
                <MenuItem value="PT">PT</MenuItem>
              </Select>
            </FormControl>
            <Box sx={{ display: 'flex', gap: 2 }}>
              <TextField
                label="Rows"
                type="number"
                value={newProtocol.layout.rows}
                onChange={(e) =>
                  setNewProtocol({
                    ...newProtocol,
                    layout: { ...newProtocol.layout, rows: parseInt(e.target.value) || 1 },
                  })
                }
                inputProps={{ min: 1, max: 4 }}
                fullWidth
              />
              <TextField
                label="Columns"
                type="number"
                value={newProtocol.layout.cols}
                onChange={(e) =>
                  setNewProtocol({
                    ...newProtocol,
                    layout: { ...newProtocol.layout, cols: parseInt(e.target.value) || 1 },
                  })
                }
                inputProps={{ min: 1, max: 4 }}
                fullWidth
              />
            </Box>
            <Box sx={{ textAlign: 'center' }}>
              {renderLayoutPreview(newProtocol.layout)}
            </Box>
          </Box>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setShowCreateDialog(false)}>Cancel</Button>
          <Button
            variant="contained"
            onClick={handleSaveProtocol}
            disabled={!newProtocol.name}
            startIcon={<SaveIcon />}
          >
            Save Protocol
          </Button>
        </DialogActions>
      </Dialog>
    </>
  );
};

export default HangingProtocolSelector;
