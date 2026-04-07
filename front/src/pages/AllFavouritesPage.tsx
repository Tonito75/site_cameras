import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import {
  Container, Stack, Avatar, Typography, Paper,
  Button, CircularProgress, Alert, Box, IconButton, Tooltip, Chip
} from '@mui/material'
import { Star, ArrowBack, Delete } from '@mui/icons-material'
import { api } from '../api/client'
import type { FavouriteWithCamera } from '../types/camera'

const PAGE_SIZE = 20

export default function AllFavouritesPage() {
  const navigate = useNavigate()
  const [items, setItems] = useState<FavouriteWithCamera[]>([])
  const [total, setTotal] = useState(0)
  const [loading, setLoading] = useState(true)
  const [loadingMore, setLoadingMore] = useState(false)

  const load = async () => {
    setLoading(true)
    try {
      const data = await api.getAllFavourites(0, PAGE_SIZE)
      setItems(data.items)
      setTotal(data.total)
    } finally {
      setLoading(false)
    }
  }

  const loadMore = async () => {
    setLoadingMore(true)
    try {
      const data = await api.getAllFavourites(items.length, PAGE_SIZE)
      setItems(prev => [...prev, ...data.items])
      setTotal(data.total)
    } finally {
      setLoadingMore(false)
    }
  }

  useEffect(() => { load() }, [])

  const handleDelete = async (id: number) => {
    await api.deleteFavourite(id)
    setItems(prev => prev.filter(f => f.id !== id))
    setTotal(prev => prev - 1)
  }

  const hasMore = items.length < total

  return (
    <Container maxWidth="lg" sx={{ py: 8 }}>
      <Stack spacing={4} alignItems="center">
        <Stack alignItems="center" spacing={1}>
          <Stack direction="row" alignItems="center" spacing={2}>
            <Avatar sx={{ bgcolor: 'warning.main', width: 48, height: 48 }}>
              <Star />
            </Avatar>
            <Typography variant="h5" fontWeight="bold">Les meilleures images</Typography>
          </Stack>
          <Typography variant="subtitle1" color="text.secondary">
            Toutes les caméras confondues
          </Typography>
        </Stack>

        <Button
          variant="outlined"
          startIcon={<ArrowBack />}
          size="small"
          onClick={() => navigate('/')}
        >
          Retour
        </Button>

        <Paper
          elevation={0}
          sx={{
            p: 3,
            width: '100%',
            background: 'rgba(255,255,255,0.05)',
            border: '1px solid rgba(255,255,255,0.1)',
          }}
        >
          {loading && (
            <Stack alignItems="center" spacing={2} py={8}>
              <CircularProgress color="primary" />
              <Typography variant="body2" color="text.secondary">Chargement...</Typography>
            </Stack>
          )}

          {!loading && items.length === 0 && (
            <Alert severity="info" variant="outlined">
              Aucun favori enregistré.
            </Alert>
          )}

          {!loading && items.length > 0 && (
            <Stack spacing={3}>
              <Box
                sx={{
                  display: 'grid',
                  gridTemplateColumns: {
                    xs: '1fr',
                    sm: 'repeat(2, 1fr)',
                    md: 'repeat(3, 1fr)',
                    lg: 'repeat(auto-fill, minmax(400px, 1fr))',
                  },
                  gap: 2,
                }}
              >
                {items.map((fav) => (
                  <Paper
                    key={fav.id}
                    elevation={2}
                    sx={{
                      p: 2,
                      background: 'rgba(255,255,255,0.05)',
                      border: '1px solid rgba(255,255,255,0.08)',
                    }}
                  >
                    <Stack spacing={1} alignItems="center">
                      <Stack direction="row" spacing={1} alignItems="center">
                        <Chip
                          label={fav.cameraName}
                          size="small"
                          variant="outlined"
                          sx={{ fontSize: '0.7rem' }}
                          onClick={() => navigate(`/camerahistory/${fav.cameraName.toLowerCase()}`)}
                        />
                        <Typography variant="caption" color="text.secondary">
                          {new Date(fav.addedAt).toLocaleString('fr-FR')}
                        </Typography>
                      </Stack>
                      <Box sx={{ position: 'relative', width: '100%' }}>
                        <Box
                          component="a"
                          href={fav.url}
                          target="_blank"
                          rel="noopener noreferrer"
                          sx={{ display: 'block', width: '100%' }}
                        >
                          <Box
                            component="img"
                            src={fav.url}
                            alt="Favori"
                            loading="lazy"
                            sx={{
                              width: '100%',
                              height: 300,
                              objectFit: 'cover',
                              borderRadius: 1,
                              transition: 'transform 0.2s',
                              '&:hover': { transform: 'scale(1.02)' },
                            }}
                          />
                        </Box>
                        <Tooltip title="Supprimer des favoris">
                          <IconButton
                            size="small"
                            onClick={() => handleDelete(fav.id)}
                            sx={{
                              position: 'absolute',
                              top: 6,
                              right: 6,
                              bgcolor: 'rgba(0,0,0,0.45)',
                              color: 'error.main',
                              '&:hover': { bgcolor: 'rgba(0,0,0,0.65)' },
                              p: '4px',
                            }}
                          >
                            <Delete fontSize="small" />
                          </IconButton>
                        </Tooltip>
                      </Box>
                    </Stack>
                  </Paper>
                ))}
              </Box>

              {hasMore && (
                <Stack alignItems="center">
                  <Button
                    variant="outlined"
                    onClick={loadMore}
                    disabled={loadingMore}
                    startIcon={loadingMore ? <CircularProgress size={16} /> : undefined}
                  >
                    {loadingMore ? 'Chargement...' : `Charger 20 de plus (${total - items.length} restants)`}
                  </Button>
                </Stack>
              )}
            </Stack>
          )}
        </Paper>

        <Typography variant="caption" color="text.secondary">
          {items.length} / {total} favori(s) affiché(s)
        </Typography>
      </Stack>
    </Container>
  )
}
