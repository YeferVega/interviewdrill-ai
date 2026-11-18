import express from 'express';
import cors from 'cors';
import { fileURLToPath } from 'url';
import { dirname, join } from 'path';
import { ArchitectureEvaluatorService } from '../dist/index.js';

const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);

const app = express();
const PORT = 3000;

// Middleware
app.use(cors());
app.use(express.json());
app.use(express.static(join(__dirname, 'public')));

// Initialize evaluator
const evaluator = new ArchitectureEvaluatorService({ enableLogging: true });

// API Endpoints
app.post('/api/evaluate', async (req, res) => {
  try {
    const { text, topic, sessionId } = req.body;

    if (!text || !topic || !sessionId) {
      return res.status(400).json({ error: 'Missing required fields: text, topic, sessionId' });
    }

    const startTime = Date.now();
    const result = await evaluator.evaluate({ text, topic, sessionId });
    const duration = Date.now() - startTime;

    res.json({
      success: true,
      result,
      performance: {
        duration: `${duration}ms`
      }
    });
  } catch (error) {
    console.error('Evaluation error:', error);
    res.status(500).json({
      success: false,
      error: error instanceof Error ? error.message : 'Unknown error'
    });
  }
});

// Health check
app.get('/api/health', (req, res) => {
  res.json({ status: 'ok', timestamp: new Date().toISOString() });
});

// Start server
app.listen(PORT, () => {
  console.log(`🚀 InterviewDrill AI Server running at http://localhost:${PORT}`);
  console.log(`📊 API available at http://localhost:${PORT}/api`);
});
