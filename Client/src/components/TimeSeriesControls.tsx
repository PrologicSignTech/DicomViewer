import React, { useState, useEffect } from 'react';
import {
  Box,
  Paper,
  Typography,
  IconButton,
  Slider,
  Button,
  TextField,
  ToggleButton,
  ToggleButtonGroup,
  Chip,
  Tooltip,
  Divider,
} from '@mui/material';
import {
  PlayArrow,
  Pause,
  Stop,
  SkipNext,
  SkipPrevious,
  FirstPage,
  LastPage,
  Loop,
  Speed,
  Timeline,
  Compare,
  Bookmark,
} from '@mui/icons-material';

interface TimeSeriesControlsProps {
  totalFrames: number;
  currentFrame: number;
  onFrameChange: (frame: number) => void;
  frameRate?: number;
  onPlayPause?: (isPlaying: boolean) => void;
}

const TimeSeriesControls: React.FC<TimeSeriesControlsProps> = ({
  totalFrames,
  currentFrame,
  onFrameChange,
  frameRate = 30,
  onPlayPause,
}) => {
  const [isPlaying, setIsPlaying] = useState(false);
  const [playbackSpeed, setPlaybackSpeed] = useState<number>(1);
  const [loopMode, setLoopMode] = useState<'once' | 'loop' | 'bounce'>('loop');
  const [playbackDirection, setPlaybackDirection] = useState<'forward' | 'backward'>('forward');
  const [keyFrames, setKeyFrames] = useState<number[]>([]);
  const [showTimeline, setShowTimeline] = useState(true);

  useEffect(() => {
    let interval: NodeJS.Timeout;
    
    if (isPlaying && totalFrames > 1) {
      const frameInterval = (1000 / frameRate) / playbackSpeed;
      
      interval = setInterval(() => {
        onFrameChange((prevFrame) => {
          let nextFrame = prevFrame;
          
          if (playbackDirection === 'forward') {
            nextFrame = prevFrame + 1;
            
            if (nextFrame >= totalFrames) {
              if (loopMode === 'loop') {
                nextFrame = 0;
              } else if (loopMode === 'bounce') {
                setPlaybackDirection('backward');
                nextFrame = totalFrames - 2;
              } else {
                setIsPlaying(false);
                nextFrame = totalFrames - 1;
              }
            }
          } else {
            nextFrame = prevFrame - 1;
            
            if (nextFrame < 0) {
              if (loopMode === 'loop') {
                nextFrame = totalFrames - 1;
              } else if (loopMode === 'bounce') {
                setPlaybackDirection('forward');
                nextFrame = 1;
              } else {
                setIsPlaying(false);
                nextFrame = 0;
              }
            }
          }
          
          return nextFrame;
        });
      }, frameInterval);
    }

    return () => clearInterval(interval);
  }, [isPlaying, totalFrames, frameRate, playbackSpeed, loopMode, playbackDirection]);

  const handlePlayPause = () => {
    const newPlayState = !isPlaying;
    setIsPlaying(newPlayState);
    onPlayPause?.(newPlayState);
  };

  const handleStop = () => {
    setIsPlaying(false);
    onFrameChange(0);
    onPlayPause?.(false);
  };

  const handleFrameChange = (_: Event, value: number | number[]) => {
    onFrameChange(value as number);
  };

  const handleFirstFrame = () => {
    onFrameChange(0);
  };

  const handleLastFrame = () => {
    onFrameChange(totalFrames - 1);
  };

  const handlePreviousFrame = () => {
    if (currentFrame > 0) {
      onFrameChange(currentFrame - 1);
    }
  };

  const handleNextFrame = () => {
    if (currentFrame < totalFrames - 1) {
      onFrameChange(currentFrame + 1);
    }
  };

  const addKeyFrame = () => {
    if (!keyFrames.includes(currentFrame)) {
      setKeyFrames([...keyFrames, currentFrame].sort((a, b) => a - b));
    }
  };

  const removeKeyFrame = (frame: number) => {
    setKeyFrames(keyFrames.filter((f) => f !== frame));
  };

  const jumpToKeyFrame = (index: number) => {
    if (index >= 0 && index < keyFrames.length) {
      onFrameChange(keyFrames[index]);
    }
  };

  const formatTime = (frame: number): string => {
    const timeInSeconds = frame / frameRate;
    const minutes = Math.floor(timeInSeconds / 60);
    const seconds = Math.floor(timeInSeconds % 60);
    const milliseconds = Math.floor((timeInSeconds % 1) * 1000);
    return `${minutes.toString().padStart(2, '0')}:${seconds.toString().padStart(2, '0')}.${milliseconds.toString().padStart(3, '0')}`;
  };

  return (
    <Box sx={{ width: '100%', p: 2 }}>
      <Typography variant="h6" sx={{ mb: 2, fontWeight: 600 }}>
        4D / Time-Series Playback
      </Typography>

      {/* Playback Controls */}
      <Paper sx={{ p: 2, mb: 2, backgroundColor: '#1a1a1a' }}>
        <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'center', mb: 2 }}>
          <IconButton onClick={handleFirstFrame} disabled={currentFrame === 0}>
            <FirstPage />
          </IconButton>
          <IconButton onClick={handlePreviousFrame} disabled={currentFrame === 0}>
            <SkipPrevious />
          </IconButton>
          <IconButton
            onClick={handlePlayPause}
            sx={{
              backgroundColor: '#4fc3f7',
              color: '#000',
              mx: 1,
              '&:hover': { backgroundColor: '#29b6f6' },
            }}
          >
            {isPlaying ? <Pause /> : <PlayArrow />}
          </IconButton>
          <IconButton onClick={handleStop}>
            <Stop />
          </IconButton>
          <IconButton onClick={handleNextFrame} disabled={currentFrame === totalFrames - 1}>
            <SkipNext />
          </IconButton>
          <IconButton onClick={handleLastFrame} disabled={currentFrame === totalFrames - 1}>
            <LastPage />
          </IconButton>
        </Box>

        {/* Frame Info */}
        <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
          <Typography variant="body2">
            Frame: {currentFrame + 1} / {totalFrames}
          </Typography>
          <Typography variant="body2" color="text.secondary">
            Time: {formatTime(currentFrame)}
          </Typography>
        </Box>

        {/* Timeline Slider */}
        {showTimeline && (
          <Box sx={{ px: 1 }}>
            <Slider
              value={currentFrame}
              onChange={handleFrameChange}
              min={0}
              max={totalFrames - 1}
              marks={keyFrames.map((frame) => ({ value: frame, label: '' }))}
              sx={{
                color: '#4fc3f7',
                '& .MuiSlider-mark': {
                  backgroundColor: '#ff4444',
                  height: 8,
                  width: 2,
                },
              }}
            />
          </Box>
        )}
      </Paper>

      {/* Playback Settings */}
      <Paper sx={{ p: 2, mb: 2, backgroundColor: '#1a1a1a' }}>
        <Typography variant="subtitle2" sx={{ mb: 1 }}>
          Playback Settings
        </Typography>

        {/* Speed Control */}
        <Box sx={{ mb: 2 }}>
          <Typography variant="caption" gutterBottom>
            Speed: {playbackSpeed}x
          </Typography>
          <Slider
            value={playbackSpeed}
            onChange={(_, value) => setPlaybackSpeed(value as number)}
            min={0.25}
            max={4}
            step={0.25}
            marks={[
              { value: 0.5, label: '0.5x' },
              { value: 1, label: '1x' },
              { value: 2, label: '2x' },
              { value: 4, label: '4x' },
            ]}
            sx={{ color: '#4fc3f7' }}
          />
        </Box>

        {/* Loop Mode */}
        <Box sx={{ mb: 2 }}>
          <Typography variant="caption" gutterBottom display="block">
            Loop Mode
          </Typography>
          <ToggleButtonGroup
            value={loopMode}
            exclusive
            onChange={(_, value) => value && setLoopMode(value)}
            size="small"
            fullWidth
          >
            <ToggleButton value="once">
              <Tooltip title="Play Once">
                <PlayArrow />
              </Tooltip>
            </ToggleButton>
            <ToggleButton value="loop">
              <Tooltip title="Loop">
                <Loop />
              </Tooltip>
            </ToggleButton>
            <ToggleButton value="bounce">
              <Tooltip title="Bounce">
                <Speed />
              </Tooltip>
            </ToggleButton>
          </ToggleButtonGroup>
        </Box>

        {/* Frame Rate */}
        <TextField
          label="Frame Rate (fps)"
          type="number"
          size="small"
          value={frameRate}
          InputProps={{ readOnly: true }}
          fullWidth
        />
      </Paper>

      {/* Key Frames */}
      <Paper sx={{ p: 2, mb: 2, backgroundColor: '#1a1a1a' }}>
        <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', mb: 1 }}>
          <Typography variant="subtitle2">Key Frames</Typography>
          <IconButton size="small" onClick={addKeyFrame}>
            <Bookmark />
          </IconButton>
        </Box>
        <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1 }}>
          {keyFrames.map((frame, index) => (
            <Chip
              key={frame}
              label={`F${frame + 1}`}
              size="small"
              onClick={() => onFrameChange(frame)}
              onDelete={() => removeKeyFrame(frame)}
              sx={{
                backgroundColor: currentFrame === frame ? '#4fc3f7' : '#333',
                color: currentFrame === frame ? '#000' : '#fff',
              }}
            />
          ))}
          {keyFrames.length === 0 && (
            <Typography variant="caption" color="text.secondary">
              No key frames marked
            </Typography>
          )}
        </Box>
      </Paper>

      {/* Multi-Phase Analysis */}
      <Paper sx={{ p: 2, backgroundColor: '#1a1a1a' }}>
        <Typography variant="subtitle2" sx={{ mb: 1 }}>
          Multi-Phase Analysis
        </Typography>
        <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1 }}>
          <Button variant="outlined" size="small" startIcon={<Timeline />}>
            Time-Intensity Curve
          </Button>
          <Button variant="outlined" size="small" startIcon={<Compare />}>
            Compare Timepoints
          </Button>
        </Box>
      </Paper>
    </Box>
  );
};

export default TimeSeriesControls;
