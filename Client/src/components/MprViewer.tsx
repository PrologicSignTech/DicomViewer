import React, { useEffect, useState } from 'react';
import { Box, Slider, Typography, IconButton, Tooltip, ToggleButton, ToggleButtonGroup, Paper, CircularProgress } from '@mui/material';
import { Layers, AspectRatio, ZoomIn, ZoomOut, Refresh } from '@mui/icons-material';

interface MprViewerProps {
  seriesId: number;
  windowCenter: number;
  windowWidth: number;
}

interface MprVolumeInfo {
  width: number;
  height: number;
  depth: number;
  pixelSpacingX: number;
  pixelSpacingY: number;
  sliceThickness: number;
}

const MprViewer: React.FC<MprViewerProps> = ({ seriesId, windowCenter, windowWidth }) => {
  const [volumeInfo, setVolumeInfo] = useState<MprVolumeInfo | null>(null);
  const [axialSlice, setAxialSlice] = useState(0);
  const [sagittalSlice, setSagittalSlice] = useState(0);
  const [coronalSlice, setCoronalSlice] = useState(0);
  const [loading, setLoading] = useState(true);
  const [projectionType, setProjectionType] = useState<'mpr' | 'mip' | 'minip' | 'average'>('mpr');
  const [slabThickness, setSlabThickness] = useState(1);

  useEffect(() => {
    loadVolumeInfo();
  }, [seriesId]);

  const loadVolumeInfo = async () => {
    setLoading(true);
    try {
      const response = await fetch(`/api/advanced-imaging/series/${seriesId}/mpr-info`);
      const data = await response.json();
      setVolumeInfo(data);
      setAxialSlice(Math.floor(data.depth / 2));
      setSagittalSlice(Math.floor(data.width / 2));
      setCoronalSlice(Math.floor(data.height / 2));
    } catch (error) {
      console.error('Error loading volume info:', error);
    } finally {
      setLoading(false);
    }
  };

  const getMprUrl = (plane: string, sliceIndex: number) => {
    if (projectionType === 'mpr') {
      return `/api/advanced-imaging/series/${seriesId}/mpr?plane=${plane}&sliceIndex=${sliceIndex}&windowCenter=${windowCenter}&windowWidth=${windowWidth}`;
    } else {
      const startSlice = Math.max(0, sliceIndex - Math.floor(slabThickness / 2));
      const endSlice = sliceIndex + Math.floor(slabThickness / 2);
      return `/api/advanced-imaging/series/${seriesId}/projection?type=${projectionType}&plane=${plane}&startSlice=${startSlice}&endSlice=${endSlice}&windowCenter=${windowCenter}&windowWidth=${windowWidth}`;
    }
  };

  if (loading) {
    return (
      <Box sx={{ flex: 1, display: 'flex', alignItems: 'center', justifyContent: 'center', backgroundColor: '#000' }}>
        <CircularProgress />
      </Box>
    );
  }

  if (!volumeInfo) {
    return (
      <Box sx={{ flex: 1, display: 'flex', alignItems: 'center', justifyContent: 'center', backgroundColor: '#000' }}>
        <Typography color="error">Failed to load volume data</Typography>
      </Box>
    );
  }

  return (
    <Box sx={{ flex: 1, display: 'flex', flexDirection: 'column', backgroundColor: '#000' }}>
      {/* MPR Toolbar */}
      <Paper sx={{ p: 1, display: 'flex', alignItems: 'center', gap: 2, backgroundColor: '#1a1a1a', borderRadius: 0 }}>
        <ToggleButtonGroup
          value={projectionType}
          exclusive
          onChange={(_, v) => v && setProjectionType(v)}
          size="small"
        >
          <ToggleButton value="mpr">
            <Tooltip title="MPR"><Layers /></Tooltip>
          </ToggleButton>
          <ToggleButton value="mip">
            <Tooltip title="MIP (Maximum Intensity)">MIP</Tooltip>
          </ToggleButton>
          <ToggleButton value="minip">
            <Tooltip title="MinIP (Minimum Intensity)">MinIP</Tooltip>
          </ToggleButton>
          <ToggleButton value="average">
            <Tooltip title="Average Intensity">AVG</Tooltip>
          </ToggleButton>
        </ToggleButtonGroup>

        {projectionType !== 'mpr' && (
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, minWidth: 200 }}>
            <Typography variant="caption">Slab:</Typography>
            <Slider
              value={slabThickness}
              onChange={(_, v) => setSlabThickness(v as number)}
              min={1}
              max={volumeInfo.depth}
              size="small"
              sx={{ width: 100 }}
            />
            <Typography variant="caption">{slabThickness}</Typography>
          </Box>
        )}

        <Tooltip title="Reset Views">
          <IconButton size="small" onClick={() => {
            setAxialSlice(Math.floor(volumeInfo.depth / 2));
            setSagittalSlice(Math.floor(volumeInfo.width / 2));
            setCoronalSlice(Math.floor(volumeInfo.height / 2));
          }}>
            <Refresh />
          </IconButton>
        </Tooltip>
      </Paper>

      {/* MPR Views Grid */}
      <Box sx={{ flex: 1, display: 'grid', gridTemplateColumns: '1fr 1fr', gridTemplateRows: '1fr 1fr', gap: '2px' }}>
        {/* Axial View (Top Left) */}
        <Box sx={{ position: 'relative', backgroundColor: '#0a0a0a', display: 'flex', flexDirection: 'column' }}>
          <Box sx={{ position: 'absolute', top: 4, left: 4, zIndex: 10 }}>
            <Typography variant="caption" sx={{ color: '#4fc3f7', fontWeight: 'bold', textShadow: '1px 1px 2px #000' }}>
              AXIAL
            </Typography>
          </Box>
          <Box sx={{ flex: 1, display: 'flex', alignItems: 'center', justifyContent: 'center', overflow: 'hidden' }}>
            <img
              src={getMprUrl('axial', axialSlice)}
              alt="Axial"
              style={{ maxWidth: '100%', maxHeight: '100%', objectFit: 'contain' }}
            />
          </Box>
          <Box sx={{ px: 2, py: 0.5, display: 'flex', alignItems: 'center', gap: 1 }}>
            <Typography variant="caption" sx={{ color: '#aaa' }}>Slice:</Typography>
            <Slider
              value={axialSlice}
              onChange={(_, v) => setAxialSlice(v as number)}
              min={0}
              max={volumeInfo.depth - 1}
              size="small"
              sx={{ flex: 1 }}
            />
            <Typography variant="caption" sx={{ color: '#aaa', minWidth: 50 }}>{axialSlice + 1}/{volumeInfo.depth}</Typography>
          </Box>
          {/* Crosshairs indicator */}
          <Box sx={{
            position: 'absolute',
            top: '50%',
            left: `${(sagittalSlice / volumeInfo.width) * 100}%`,
            width: 1,
            height: '100%',
            backgroundColor: 'rgba(255, 0, 0, 0.5)',
            transform: 'translateY(-50%)',
            pointerEvents: 'none'
          }} />
          <Box sx={{
            position: 'absolute',
            top: `${(coronalSlice / volumeInfo.height) * 100}%`,
            left: 0,
            width: '100%',
            height: 1,
            backgroundColor: 'rgba(0, 255, 0, 0.5)',
            pointerEvents: 'none'
          }} />
        </Box>

        {/* Sagittal View (Top Right) */}
        <Box sx={{ position: 'relative', backgroundColor: '#0a0a0a', display: 'flex', flexDirection: 'column' }}>
          <Box sx={{ position: 'absolute', top: 4, left: 4, zIndex: 10 }}>
            <Typography variant="caption" sx={{ color: '#ff6b6b', fontWeight: 'bold', textShadow: '1px 1px 2px #000' }}>
              SAGITTAL
            </Typography>
          </Box>
          <Box sx={{ flex: 1, display: 'flex', alignItems: 'center', justifyContent: 'center', overflow: 'hidden' }}>
            <img
              src={getMprUrl('sagittal', sagittalSlice)}
              alt="Sagittal"
              style={{ maxWidth: '100%', maxHeight: '100%', objectFit: 'contain' }}
            />
          </Box>
          <Box sx={{ px: 2, py: 0.5, display: 'flex', alignItems: 'center', gap: 1 }}>
            <Typography variant="caption" sx={{ color: '#aaa' }}>Slice:</Typography>
            <Slider
              value={sagittalSlice}
              onChange={(_, v) => setSagittalSlice(v as number)}
              min={0}
              max={volumeInfo.width - 1}
              size="small"
              sx={{ flex: 1 }}
            />
            <Typography variant="caption" sx={{ color: '#aaa', minWidth: 50 }}>{sagittalSlice + 1}/{volumeInfo.width}</Typography>
          </Box>
        </Box>

        {/* Coronal View (Bottom Left) */}
        <Box sx={{ position: 'relative', backgroundColor: '#0a0a0a', display: 'flex', flexDirection: 'column' }}>
          <Box sx={{ position: 'absolute', top: 4, left: 4, zIndex: 10 }}>
            <Typography variant="caption" sx={{ color: '#69db7c', fontWeight: 'bold', textShadow: '1px 1px 2px #000' }}>
              CORONAL
            </Typography>
          </Box>
          <Box sx={{ flex: 1, display: 'flex', alignItems: 'center', justifyContent: 'center', overflow: 'hidden' }}>
            <img
              src={getMprUrl('coronal', coronalSlice)}
              alt="Coronal"
              style={{ maxWidth: '100%', maxHeight: '100%', objectFit: 'contain' }}
            />
          </Box>
          <Box sx={{ px: 2, py: 0.5, display: 'flex', alignItems: 'center', gap: 1 }}>
            <Typography variant="caption" sx={{ color: '#aaa' }}>Slice:</Typography>
            <Slider
              value={coronalSlice}
              onChange={(_, v) => setCoronalSlice(v as number)}
              min={0}
              max={volumeInfo.height - 1}
              size="small"
              sx={{ flex: 1 }}
            />
            <Typography variant="caption" sx={{ color: '#aaa', minWidth: 50 }}>{coronalSlice + 1}/{volumeInfo.height}</Typography>
          </Box>
        </Box>

        {/* Info Panel (Bottom Right) */}
        <Box sx={{ backgroundColor: '#1a1a1a', p: 2, display: 'flex', flexDirection: 'column', gap: 1 }}>
          <Typography variant="subtitle2" sx={{ color: '#4fc3f7' }}>Volume Information</Typography>
          <Box sx={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 1 }}>
            <Typography variant="caption" color="text.secondary">Dimensions:</Typography>
            <Typography variant="caption">{volumeInfo.width} x {volumeInfo.height} x {volumeInfo.depth}</Typography>
            
            <Typography variant="caption" color="text.secondary">Pixel Spacing:</Typography>
            <Typography variant="caption">{volumeInfo.pixelSpacingX.toFixed(2)} x {volumeInfo.pixelSpacingY.toFixed(2)} mm</Typography>
            
            <Typography variant="caption" color="text.secondary">Slice Thickness:</Typography>
            <Typography variant="caption">{volumeInfo.sliceThickness.toFixed(2)} mm</Typography>
            
            <Typography variant="caption" color="text.secondary">Volume Size:</Typography>
            <Typography variant="caption">
              {(volumeInfo.width * volumeInfo.pixelSpacingX / 10).toFixed(1)} x {(volumeInfo.height * volumeInfo.pixelSpacingY / 10).toFixed(1)} x {(volumeInfo.depth * volumeInfo.sliceThickness / 10).toFixed(1)} cm
            </Typography>
          </Box>

          <Box sx={{ mt: 2 }}>
            <Typography variant="subtitle2" sx={{ color: '#4fc3f7', mb: 1 }}>Current Position</Typography>
            <Typography variant="caption" display="block">
              Axial: Slice {axialSlice + 1} ({((axialSlice * volumeInfo.sliceThickness)).toFixed(1)} mm)
            </Typography>
            <Typography variant="caption" display="block">
              Sagittal: Slice {sagittalSlice + 1} ({((sagittalSlice * volumeInfo.pixelSpacingX)).toFixed(1)} mm)
            </Typography>
            <Typography variant="caption" display="block">
              Coronal: Slice {coronalSlice + 1} ({((coronalSlice * volumeInfo.pixelSpacingY)).toFixed(1)} mm)
            </Typography>
          </Box>

          <Box sx={{ mt: 2 }}>
            <Typography variant="subtitle2" sx={{ color: '#4fc3f7', mb: 1 }}>Keyboard Shortcuts</Typography>
            <Typography variant="caption" color="text.secondary" display="block">Mouse wheel: Scroll slices</Typography>
            <Typography variant="caption" color="text.secondary" display="block">Click on view: Set crosshair</Typography>
            <Typography variant="caption" color="text.secondary" display="block">M: Toggle MPR mode</Typography>
          </Box>
        </Box>
      </Box>
    </Box>
  );
};

export default MprViewer;
