using AutoMapper;
using LearningDocumentSystem.Business.DTOs;
using LearningDocumentSystem.Business.Services.Interfaces;
using LearningDocumentSystem.Common.Exceptions;
using LearningDocumentSystem.Data.Repositories.Interfaces;
using LearningDocumentSystem.Entities.Models;
using Microsoft.Extensions.Logging;

namespace LearningDocumentSystem.Business.Services.Implementations
{
    public class ChapterService : IChapterService
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;
        private readonly ILogger<ChapterService> _logger;

        public ChapterService(IUnitOfWork uow, IMapper mapper, ILogger<ChapterService> logger)
        {
            _uow = uow;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<IEnumerable<ChapterDto>> GetAllAsync()
        {
            var chapters = await _uow.Chapters.GetAllAsync();
            return _mapper.Map<IEnumerable<ChapterDto>>(chapters);
        }

        public async Task<IEnumerable<ChapterDto>> GetBySubjectAsync(int subjectId)
        {
            var chapters = await _uow.Chapters.GetBySubjectIdAsync(subjectId);
            return _mapper.Map<IEnumerable<ChapterDto>>(chapters);
        }

        public async Task<ChapterDto?> GetByIdAsync(int id)
        {
            var chapter = await _uow.Chapters.GetByIdAsync(id);
            return chapter == null ? null : _mapper.Map<ChapterDto>(chapter);
        }

        public async Task<ChapterDto> CreateAsync(CreateChapterDto dto)
        {
            if (!await _uow.Subjects.AnyAsync(s => s.SubjectID == dto.SubjectID))
                throw new NotFoundException("Subject", dto.SubjectID);

            if (await _uow.Chapters.IsChapterNumberExistsAsync(dto.SubjectID, dto.ChapterNumber))
                throw new BusinessException($"Chương {dto.ChapterNumber} đã tồn tại trong môn học này.");

            var chapter = _mapper.Map<Chapter>(dto);
            await _uow.Chapters.AddAsync(chapter);
            await _uow.SaveChangesAsync();

            _logger.LogInformation("Chapter created: {Name}", dto.ChapterName);
            return _mapper.Map<ChapterDto>(chapter);
        }

        public async Task<ChapterDto> UpdateAsync(UpdateChapterDto dto)
        {
            var chapter = await _uow.Chapters.GetByIdAsync(dto.ChapterID)
                ?? throw new NotFoundException("Chapter", dto.ChapterID);

            if (await _uow.Chapters.IsChapterNumberExistsAsync(dto.SubjectID, dto.ChapterNumber, dto.ChapterID))
                throw new BusinessException($"Chương {dto.ChapterNumber} đã tồn tại.");

            chapter.ChapterName = dto.ChapterName;
            chapter.ChapterNumber = dto.ChapterNumber;
            chapter.SubjectID = dto.SubjectID;
            _uow.Chapters.Update(chapter);
            await _uow.SaveChangesAsync();

            return _mapper.Map<ChapterDto>(chapter);
        }

        public async Task DeleteAsync(int id)
        {
            var chapter = await _uow.Chapters.GetByIdAsync(id)
                ?? throw new NotFoundException("Chapter", id);

            _uow.Chapters.Remove(chapter);
            await _uow.SaveChangesAsync();
        }
    }
}