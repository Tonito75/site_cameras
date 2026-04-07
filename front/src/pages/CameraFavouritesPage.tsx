import { useEffect, useState } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import {
  Container, Stack, Avatar, Typography, Paper,
  Button, CircularProgress, Alert, Box, IconButton, Tooltip
} from '@mui/material'
import { Favorite, ArrowBack, History, Delete } from '@mui/icons-material'
import { api } from '../api/client'
import type { Favourite } from '../types/camera'

const PAGE_SIZE = 20

export default function CameraFavouritesPage() {
  const { name } = useParams<{ name: string }>()
  const navigate = useNavigate()
  const [favourites, setFavourites] = useState<Favourite[]>([])
  const [total, setTotal] = useState(0)
  const [loading, setLoading] = useState(true)
  const [loadingMore, setLoadingMore] = useState(false)

  const load = async () => {
    setLoading(true)
    try {
      const data = await api.getCameraFavourites(name!, 0, PAGE_SIZE)
      setFavourites(data.items)
      setTotal(data.total)
    } finally {
      setLoading(false)
    }
  }

  const loadMore = async () => {
    setLoadingMore(true)
    try {
      const data = await api.getCameraFavourites(name!, favourites.length, PAGE_SIZE)
      setFavourites(prev => [...prev, ...data.items])
      setTotal(data.total)
    } finally {
      setLoadingMore(false)
    }
  }

  useEffect(() => { load() }, [name])

  const handleDelete = async (id: number) => {
    await api.deleteFavourite(id)
    setFavourites(prev => prev.filter(f => f.id !== id))
    setTotal(prev => prev - 1)
  }

  const hasMore = favourites.length < total

  const navButtons = (
    <Stack direction="row" spacing={2}>
      <Button variant="outlined" startIcon={<ArrowBack />} size="small" onClick={() => navigate('/')}>
        Retour
      </Button>
      <Button variant="outlined" startIcon={<History />} size="small" onClick={() => navigate(`/camerahistory/${name}`)}>
        Historique
      </Button>
    </Stack>
  )

  return (
    <Container maxWidth="lg" sx={{ py: 8 }}>
      <Stack spacing={4} alignItems="center">
        <Stack alignItems="center" spacing={1}>
          <Stack direction="row" alignItems="center" spacing={2}>
            <Avatar sx={{ bgcolor: 'warning.main', width: 48, height: 48 }}>
              <Favorite />
            </Avatar>
            <Typography variant="h5" fontWeight="bold">Favoris - {name}</Typography>
          </Stack>
          <Typography variant="subtitle1" color="text.secondary">
            Images sauvegardées
          </Typography>
        </Stack>

        {navButtons}

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
              <Typography variant="body2" color="text.secondary">Chargement des favoris...</Typography>
            </Stack>
          )}

          {!loading && favourites.length === 0 && (
            <Alert severity="info" variant="outlined">
              Aucun favori pour cette caméra.
            </Alert>
          )}

          {!loading && favourites.length > 0 && (
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
                {favourites.map((fav) => (
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
                      <Typography variant="caption" color="text.secondary">
                        Ajouté le {new Date(fav.addedAt).toLocaleString('fr-FR')}
                      </Typography>
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
                    {loadingMore ? 'Chargement...' : `Charger 20 de plus (${total - favourites.length} restants)`}
                  </Button>
                </Stack>
              )}
            </Stack>
          )}
        </Paper>

        <Stack spacing={2} alignItems="center">
          <Typography variant="caption" color="text.secondary">
            {favourites.length} / {total} favori(s) affiché(s)
          </Typography>
          {favourites.length > PAGE_SIZE && navButtons}
        </Stack>
      </Stack>
    </Container>
  )
}
