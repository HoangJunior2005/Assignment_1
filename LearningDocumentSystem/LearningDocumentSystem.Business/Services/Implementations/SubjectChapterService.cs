using AutoMapper;
using LearningDocumentSystem.Business.DTOs;
using LearningDocumentSystem.Business.Services.Interfaces;
using LearningDocumentSystem.Common.Exceptions;
using LearningDocumentSystem.Data.Repositories.Interfaces;
using LearningDocumentSystem.Entities.Models;
using Microsoft.Extensions.Logging;

namespace LearningDocumentSystem.Business.Services.Implementations
{
    public class SubjectService : ISubjectService
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;
        private readonly ILogger<SubjectService> _logger;

        public SubjectService(IUnitOfWork uow, IMapper mapper, ILogger<SubjectService> logger)
        {
            _uow    = uow;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<IEnumerable<SubjectDto>> GetAllAsync()
        {
            var subjects = await _uow.Subjects.GetAllAsync();
            return _mapper.Map<IEnumerable<SubjectDto>>(subjects);
        }

        public async Task<SubjectDto?> GetByIdAsync(int id)
        {
            var subject = await _uow.Subjects.GetByIdAsync(id);
            return subject == null ? null : _mapper.Map<SubjectDto>(subject);
        }

        public async Task<SubjectDto?> GetWithChaptersAsync(int id)
        {
            var subject = await _uow.Subjects.GetWithChaptersAsync(id);
            return subject == null ? null : _mapper.Map<SubjectDto>(subject);
        }

        public async Task<SubjectDto> CreateAsync(CreateSubjectDto dto)
        {
            if (await _uow.Subjects.IsCodeExistsAsync(dto.SubjectCode))
                throw new BusinessException($"Mã học phần '{dto.SubjectCode}' đã tồn tại.");

            var subject = _mapper.Map<Subject>(dto);
            subject.CreatedAt = DateTime.UtcNow;
            await _uow.Subjects.AddAsync(subject);
            await _uow.SaveChangesAsync();

            _logger.LogInformation("Subject created: {Code}", dto.SubjectCode);
            return _mapper.Map<SubjectDto>(subject);
        }

        public async Task<SubjectDto> UpdateAsync(UpdateSubjectDto dto)
        {
            var subject = await _uow.Subjects.GetByIdAsync(dto.SubjectID)
                ?? throw new NotFoundException("Subject", dto.SubjectID);

            if (await _uow.Subjects.IsCodeExistsAsync(dto.SubjectCode, dto.SubjectID))
                throw new BusinessException($"Mã học phần '{dto.SubjectCode}' đã tồn tại.");

            subject.SubjectName = dto.SubjectName;
            subject.SubjectCode = dto.SubjectCode;
            _uow.Subjects.Update(subject);
            await _uow.SaveChangesAsync();

            return _mapper.Map<SubjectDto>(subject);
        }

        public async Task DeleteAsync(int id)
        {
            var subject = await _uow.Subjects.GetByIdAsync(id)
                ?? throw new NotFoundException("Subject", id);

            _uow.Subjects.Remove(subject);
            await _uow.SaveChangesAsync();
            _logger.LogInformation("Subject deleted: {Id}", id);
        }
    }

    public class ChapterService : IChapterService
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;
        private readonly ILogger<ChapterService> _logger;

        public ChapterService(IUnitOfWork uow, IMapper mapper, ILogger<ChapterService> logger)
        {
            _uow    = uow;
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

            chapter.ChapterName   = dto.ChapterName;
            chapter.ChapterNumber = dto.ChapterNumber;
            chapter.SubjectID     = dto.SubjectID;
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
